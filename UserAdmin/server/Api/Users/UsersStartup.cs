using UserAdmin.Api.Users.DataService;

namespace UserAdmin.Api.Users;

public static class UsersStartup
{
    public static void AddUsers(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<UserDataService>();
    }
}