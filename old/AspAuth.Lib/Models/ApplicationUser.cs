using Microsoft.AspNetCore.Identity;

namespace AspAuth.Lib.Models;

public class ApplicationUser : IdentityUser
{
    public UserProfile? UserProfile { get; set; }
}