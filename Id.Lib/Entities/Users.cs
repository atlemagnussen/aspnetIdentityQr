using Microsoft.AspNetCore.Identity;

namespace Id.Lib.Entities;

public class ApplicationUser : IdentityUser
{
    public UserProfile? UserProfile { get; set; }
}

/// <summary>1:1 with <see cref="ApplicationUser"/>, keyed by UserId. Table "UserProfile".</summary>
public class UserProfile
{
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string? FullName { get; set; }
}

/// <summary>System roles: always exist and can't be renamed or deleted. Other roles are free-form.</summary>
public enum UserRoles
{
    Admin
}
