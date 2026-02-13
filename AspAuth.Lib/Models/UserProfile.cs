namespace AspAuth.Lib.Models;

public class UserProfile
{
    public required string AspNetUserId { get;set;}
    public string? FullName { get; set; }
}