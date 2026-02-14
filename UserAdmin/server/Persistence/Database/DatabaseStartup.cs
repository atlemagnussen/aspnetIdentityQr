using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Identity;

namespace UserAdmin.Persistence.Database;

public static class DatabaseStartup
{
    public static void AddDatabase(this WebApplicationBuilder builder, string connectionString)
    {
        builder.AddApplicationDatabaseContext(connectionString);

        builder.Services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        
        //builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            // .AddEntityFrameworkStores<ApplicationDbContext>()
            //.AddRoleManager<RoleManager<IdentityRole>>();
    }
}