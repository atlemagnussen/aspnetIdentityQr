namespace AspAuth.Lib.Models;

public class UserProfile
{
    public required ApplicationUser User { get; set; }
    public required string UserId { get;set;}
    public string? FullName { get; set; }
}