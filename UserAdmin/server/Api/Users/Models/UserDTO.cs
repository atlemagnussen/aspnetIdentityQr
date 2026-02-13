namespace UserAdmin.Api.Users.Models;

public class UserDTO
{
    public required string Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? FullName { get; set; }
}