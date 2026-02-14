using AspAuth.Lib.Models;
using UserAdmin.Api.Users.Models;

namespace UserAdmin.Api.Users.Translators;

public class ApplicationUserToUserDTO
{
    public static UserDTO Translate(ApplicationUser user)
    {
        return new UserDTO
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            FullName = user.UserProfile?.FullName
        };
    }

    public static List<UserDTO> Translate(List<ApplicationUser> users)
    {
        return [.. users.Select(u => Translate(u))];
    }
}