using AspAuth.Lib.Models;
using UserAdmin.Api.Users.Models;

namespace UserAdmin.Api.Users.Translators;

public class ApplicationUserToUserDTO
{
    public UserDTO Translate(ApplicationUser applicationUser)
    {
        return new UserDTO
        {
            Id = applicationUser.Id,
            UserName = applicationUser.UserName!,
            Email = applicationUser.Email!,
            FullName = applicationUser.UserProfile?.FullName
        };
    }
}