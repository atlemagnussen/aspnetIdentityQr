using Id.Admin.Api.Roles;

namespace Id.Admin.Api.Users;

public static class UsersStartup
{
    public static void AddUsers(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<UsersService>();
        builder.Services.AddScoped<RolesService>();
    }
}
