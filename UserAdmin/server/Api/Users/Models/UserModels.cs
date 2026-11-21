namespace UserAdmin.Api.Users.Models;

public class UserListItemDto
{
    public required string Id { get; set; }
    public required string UserName { get; set; }
    public string? Email { get; set; }
    public required bool EmailConfirmed { get; set; }
    public string? FullName { get; set; }
    public required List<string> Roles { get; set; }
    /// <summary>Provider display names (or provider ids) of linked external logins</summary>
    public required List<string> ExternalLogins { get; set; }
    public required bool HasPassword { get; set; }
    public required bool TwoFactorEnabled { get; set; }
    public required bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}

public class UserDetailsDto : UserListItemDto
{
    public string? PhoneNumber { get; set; }
    public required bool PhoneNumberConfirmed { get; set; }
    public required bool LockoutEnabled { get; set; }
    public required int AccessFailedCount { get; set; }
    public required List<ClaimDto> Claims { get; set; }
    public required List<UserLoginDto> Logins { get; set; }
    public required List<PasskeyDto> Passkeys { get; set; }
}

public class ClaimDto
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}

public class UserLoginDto
{
    public required string LoginProvider { get; set; }
    public required string ProviderKey { get; set; }
    public string? ProviderDisplayName { get; set; }
}

public class PasskeyDto
{
    /// <summary>Base64url credential id</summary>
    public required string CredentialId { get; set; }
    public string? Name { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

public class UserUpdateDto
{
    public required string UserName { get; set; }
    public string? Email { get; set; }
    public required bool EmailConfirmed { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public required bool LockoutEnabled { get; set; }
}

public class UserCreateDto
{
    public required string Email { get; set; }
    /// <summary>Defaults to the email</summary>
    public string? UserName { get; set; }
    public string? FullName { get; set; }
}

public class UserCreatedDto
{
    public required UserDetailsDto User { get; set; }
    public required PasswordLinkDto PasswordLink { get; set; }
}

public class PasswordLinkDto
{
    public required string Url { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}

public class LockUserDto
{
    /// <summary>null = locked until unlocked</summary>
    public DateTimeOffset? Until { get; set; }
}
