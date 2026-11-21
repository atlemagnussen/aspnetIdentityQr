using System.Security.Claims;
using System.Text;
using Id.Lib.Data;
using Id.Lib.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Id.Admin.Api.Users.Models;
using Id.Admin.Auth;

namespace Id.Admin.Api.Users;

/// <summary>
/// User administration. Goes through UserManager wherever possible so normalization, security stamps
/// and validation behave exactly like on the ID server.
/// </summary>
public class UsersService(
    IdDbContext db,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles,
    AdminOptions adminOptions,
    IOptions<DataProtectionTokenProviderOptions> tokenOptions)
{
    private static readonly string AdminRole = UserRoles.Admin.ToString();

    public async Task<List<UserListItemDto>> List()
    {
        var all = await db.Users.AsNoTracking()
            .Include(u => u.UserProfile)
            .OrderBy(u => u.UserName)
            .ToListAsync();

        var userRoles = (await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                select new { ur.UserId, r.Name })
            .ToListAsync())
            .ToLookup(x => x.UserId, x => x.Name!);

        var logins = (await db.UserLogins
                .Select(l => new { l.UserId, Name = l.ProviderDisplayName ?? l.LoginProvider })
                .ToListAsync())
            .ToLookup(x => x.UserId, x => x.Name);

        return [.. all.Select(u => Fill(new UserListItemDto
        {
            Id = u.Id,
            UserName = u.UserName ?? "",
            EmailConfirmed = u.EmailConfirmed,
            Roles = [.. userRoles[u.Id].Order()],
            ExternalLogins = [.. logins[u.Id]],
            HasPassword = u.PasswordHash is not null,
            TwoFactorEnabled = u.TwoFactorEnabled,
            IsLockedOut = IsLockedOut(u),
        }, u))];
    }

    public async Task<UserDetailsDto> Get(string userId)
    {
        var user = await FindUser(userId);
        await db.Entry(user).Reference(u => u.UserProfile).LoadAsync();

        var claims = await users.GetClaimsAsync(user);
        var logins = await users.GetLoginsAsync(user);
        var passkeys = users.SupportsUserPasskey ? await users.GetPasskeysAsync(user) : [];

        return Fill(new UserDetailsDto
        {
            Id = user.Id,
            UserName = user.UserName ?? "",
            EmailConfirmed = user.EmailConfirmed,
            Roles = [.. (await users.GetRolesAsync(user)).Order()],
            ExternalLogins = [.. logins.Select(l => l.ProviderDisplayName ?? l.LoginProvider)],
            HasPassword = user.PasswordHash is not null,
            TwoFactorEnabled = user.TwoFactorEnabled,
            IsLockedOut = IsLockedOut(user),
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            Claims = [.. claims.Select(c => new ClaimDto { Type = c.Type, Value = c.Value })],
            Logins = [.. logins.Select(l => new UserLoginDto
            {
                LoginProvider = l.LoginProvider,
                ProviderKey = l.ProviderKey,
                ProviderDisplayName = l.ProviderDisplayName
            })],
            Passkeys = [.. passkeys.Select(p => new PasskeyDto
            {
                CredentialId = WebEncoders.Base64UrlEncode(p.CredentialId),
                Name = p.Name,
                CreatedAt = p.CreatedAt
            })],
        }, user);
    }

    public async Task<UserCreatedDto> Create(UserCreateDto dto)
    {
        var email = RequireText(dto.Email, "Email");
        var userName = string.IsNullOrWhiteSpace(dto.UserName) ? email : dto.UserName.Trim();

        await EnsureEmailAvailable(email, exceptUserId: null);

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true, // created by an admin; they vouch for the address
            LockoutEnabled = true,
        };
        if (!string.IsNullOrWhiteSpace(dto.FullName))
            user.UserProfile = new UserProfile { User = user, UserId = user.Id, FullName = dto.FullName.Trim() };

        Ensure(await users.CreateAsync(user));

        return new UserCreatedDto
        {
            User = await Get(user.Id),
            PasswordLink = await CreatePasswordLink(user.Id),
        };
    }

    public async Task<UserDetailsDto> Update(string userId, UserUpdateDto dto)
    {
        var user = await FindUser(userId);
        await db.Entry(user).Reference(u => u.UserProfile).LoadAsync();

        var userName = RequireText(dto.UserName, "User name");
        if (userName != user.UserName)
            Ensure(await users.SetUserNameAsync(user, userName));

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            if (email is not null)
                await EnsureEmailAvailable(email, exceptUserId: user.Id);
            Ensure(await users.SetEmailAsync(user, email));
        }

        var phone = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        if (phone != user.PhoneNumber)
            Ensure(await users.SetPhoneNumberAsync(user, phone));

        user.EmailConfirmed = email is not null && dto.EmailConfirmed;
        user.LockoutEnabled = dto.LockoutEnabled;

        var fullName = string.IsNullOrWhiteSpace(dto.FullName) ? null : dto.FullName.Trim();
        if (user.UserProfile is null && fullName is not null)
            user.UserProfile = new UserProfile { User = user, UserId = user.Id, FullName = fullName };
        else if (user.UserProfile is not null)
            user.UserProfile.FullName = fullName;

        Ensure(await users.UpdateAsync(user));
        return await Get(user.Id);
    }

    public async Task Delete(string userId, string currentUserId)
    {
        if (userId == currentUserId)
            throw new ApplicationException("You can't delete yourself.");
        var user = await FindUser(userId);
        Ensure(await users.DeleteAsync(user)); // profile, roles, claims, logins, passkeys cascade
    }

    // ---- roles ----

    public async Task<UserDetailsDto> AddRole(string userId, string role)
    {
        var user = await FindUser(userId);
        var roleEntity = await roles.FindByNameAsync(role)
            ?? throw new ApplicationException($"Role '{role}' does not exist. Create it under Roles first.");
        if (!await users.IsInRoleAsync(user, roleEntity.Name!))
            Ensure(await users.AddToRoleAsync(user, roleEntity.Name!));
        return await Get(userId);
    }

    public async Task<UserDetailsDto> RemoveRole(string userId, string role, string currentUserId)
    {
        if (userId == currentUserId && string.Equals(role, AdminRole, StringComparison.OrdinalIgnoreCase))
            throw new ApplicationException("You can't remove your own Admin role.");
        var user = await FindUser(userId);
        if (await users.IsInRoleAsync(user, role))
            Ensure(await users.RemoveFromRoleAsync(user, role));
        return await Get(userId);
    }

    // ---- claims ----

    public async Task<UserDetailsDto> AddClaim(string userId, ClaimDto dto)
    {
        var type = RequireText(dto.Type, "Claim type");
        var value = RequireText(dto.Value, "Claim value");
        if (type == "role")
            throw new ApplicationException("Use roles instead of 'role' claims.");

        var user = await FindUser(userId);
        var existing = await users.GetClaimsAsync(user);
        if (!existing.Any(c => c.Type == type && c.Value == value))
            Ensure(await users.AddClaimAsync(user, new Claim(type, value)));
        return await Get(userId);
    }

    public async Task<UserDetailsDto> RemoveClaim(string userId, string type, string value)
    {
        var user = await FindUser(userId);
        Ensure(await users.RemoveClaimAsync(user, new Claim(type, value)));
        return await Get(userId);
    }

    // ---- security ----

    public async Task<UserDetailsDto> Lock(string userId, DateTimeOffset? until, string currentUserId)
    {
        if (userId == currentUserId)
            throw new ApplicationException("You can't lock yourself out.");
        var user = await FindUser(userId);
        if (until is { } u && u <= DateTimeOffset.UtcNow)
            throw new ApplicationException("Lock end must be in the future.");

        Ensure(await users.SetLockoutEnabledAsync(user, true));
        Ensure(await users.SetLockoutEndDateAsync(user, until ?? DateTimeOffset.MaxValue));
        Ensure(await users.UpdateSecurityStampAsync(user)); // end existing ID server sessions
        return await Get(userId);
    }

    public async Task<UserDetailsDto> Unlock(string userId)
    {
        var user = await FindUser(userId);
        Ensure(await users.SetLockoutEndDateAsync(user, null));
        Ensure(await users.ResetAccessFailedCountAsync(user));
        return await Get(userId);
    }

    public async Task<UserDetailsDto> ResetTwoFactor(string userId)
    {
        var user = await FindUser(userId);
        Ensure(await users.SetTwoFactorEnabledAsync(user, false));
        Ensure(await users.ResetAuthenticatorKeyAsync(user));
        return await Get(userId);
    }

    public async Task<UserDetailsDto> RemoveLogin(string userId, string loginProvider, string providerKey)
    {
        var user = await FindUser(userId);
        Ensure(await users.RemoveLoginAsync(user, loginProvider, providerKey));
        return await Get(userId);
    }

    public async Task<UserDetailsDto> RemovePasskey(string userId, string credentialId)
    {
        var user = await FindUser(userId);
        byte[] id;
        try { id = WebEncoders.Base64UrlDecode(credentialId); }
        catch (FormatException) { throw new ApplicationException("Invalid credential id."); }
        Ensure(await users.RemovePasskeyAsync(user, id));
        return await Get(userId);
    }

    public async Task<UserDetailsDto> SignOutEverywhere(string userId)
    {
        var user = await FindUser(userId);
        Ensure(await users.UpdateSecurityStampAsync(user));
        return await Get(userId);
    }

    /// <summary>
    /// One-time link to the ID server's reset-password page. Works for users without a password too
    /// (external-login users can add one this way). Token is protected with the shared key ring.
    /// </summary>
    public async Task<PasswordLinkDto> CreatePasswordLink(string userId)
    {
        var user = await FindUser(userId);
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ApplicationException("The user needs an email address; the reset page asks for it.");

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        return new PasswordLinkDto
        {
            Url = $"{adminOptions.AuthServer.TrimEnd('/')}/Identity/Account/ResetPassword?code={code}",
            ExpiresAt = DateTimeOffset.UtcNow + tokenOptions.Value.TokenLifespan,
        };
    }

    // ---- helpers ----

    private async Task<ApplicationUser> FindUser(string userId) =>
        await users.FindByIdAsync(userId) ?? throw new ApplicationException($"User {userId} not found.");

    private async Task EnsureEmailAvailable(string email, string? exceptUserId)
    {
        // RequireUniqueEmail is off on the ID server, but FindByEmail (login, reset) breaks on duplicates
        var normalized = users.NormalizeEmail(email);
        var taken = await db.Users.AnyAsync(u => u.NormalizedEmail == normalized && u.Id != exceptUserId);
        if (taken)
            throw new ApplicationException($"Another user already has the email {email}.");
    }

    private static bool IsLockedOut(ApplicationUser u) =>
        u.LockoutEnabled && u.LockoutEnd is { } end && end > DateTimeOffset.UtcNow;

    private static T Fill<T>(T dto, ApplicationUser u) where T : UserListItemDto
    {
        dto.Email = u.Email;
        dto.FullName = u.UserProfile?.FullName;
        dto.LockoutEnd = u.LockoutEnd;
        return dto;
    }

    private static string RequireText(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ApplicationException($"{name} is required.") : value.Trim();

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ApplicationException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
