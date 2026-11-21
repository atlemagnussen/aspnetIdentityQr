using UserAdmin.Api.Roles;

namespace UserAdmin.Api.Users;

public static class UsersStartup
{
    public static void AddUsers(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UsersService>();
        builder.Services.AddScoped<RolesService>();
    }
}
