using AspAuth.Lib.Models;

namespace UserAdmin.Api.Users.Models;

public class UserRoleDTO
{
    public required string UserId { get; set; }
    public required UserRoles Role { get; set; }
}