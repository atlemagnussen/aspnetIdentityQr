using System.Security.Claims;
using Id.Lib.Data;
using Id.Lib.Entities;
using Id.Lib.Oidc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Id.Server.Oidc.Users;

/// <summary>A user with everything tokens and userinfo can say about them.</summary>
public sealed record UserData(ApplicationUser User, IReadOnlyList<Claim> Claims);

/// <summary>
/// The user's claims, the same set the old server produced: sub, name / preferred_username (user name),
/// email(_verified), phone_number(_verified), role per Identity role, fullname from UserProfile, plus
/// the user's stored claims (AspNetUserClaims).
/// </summary>
public class UserClaimsService(IdDbContext db, UserManager<ApplicationUser> users, TimeProvider time)
{
    public async Task<UserData?> FindActiveAsync(string subjectId)
    {
        var user = await db.Users.Include(u => u.UserProfile).SingleOrDefaultAsync(u => u.Id == subjectId);
        if (user is null || IsLockedOut(user))
            return null;

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, user.Id),
        };
        if (!string.IsNullOrEmpty(user.UserName))
        {
            claims.Add(new(JwtClaimTypes.Name, user.UserName));
            claims.Add(new("preferred_username", user.UserName));
        }
        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new(JwtClaimTypes.Email, user.Email));
            claims.Add(new(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }
        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new("phone_number", user.PhoneNumber));
            claims.Add(new("phone_number_verified", user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }
        if (!string.IsNullOrWhiteSpace(user.UserProfile?.FullName))
            claims.Add(new(JwtClaimTypes.FullName, user.UserProfile.FullName));
        foreach (var role in await users.GetRolesAsync(user))
            claims.Add(new(JwtClaimTypes.Role, role));
        foreach (var stored in await users.GetClaimsAsync(user))
            if (!claims.Any(c => c.Type == stored.Type && c.Value == stored.Value))
                claims.Add(new(stored.Type, stored.Value));

        return new UserData(user, claims);
    }

    private bool IsLockedOut(ApplicationUser user) =>
        user.LockoutEnabled && user.LockoutEnd is { } end && end > time.GetUtcNow();

    /// <summary>
    /// Claims for the id token (when the client asks for them) and userinfo: the claim types of the
    /// requested identity resources, plus fullname and role, which the old profile service always added.
    /// </summary>
    public static IEnumerable<Claim> ForIdentity(UserData user, ICollection<string> requestedTypes) =>
        user.Claims.Where(c => c.Type != JwtClaimTypes.Subject
            && (requestedTypes.Contains(c.Type) || c.Type is JwtClaimTypes.FullName or JwtClaimTypes.Role));

    /// <summary>Claims for access tokens: only the types the API scopes/resources ask for.</summary>
    public static IEnumerable<Claim> ForAccessToken(UserData user, ICollection<string> requestedTypes) =>
        user.Claims.Where(c => c.Type != JwtClaimTypes.Subject && requestedTypes.Contains(c.Type));
}
