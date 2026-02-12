using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Models;

[Owned]
public class UserProfile
{
    public string? FullName { get; set; }
}