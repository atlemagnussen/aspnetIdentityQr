using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using AspAuth.Lib.Services;
using Microsoft.AspNetCore.Identity;

namespace UserAdmin.Persistence.Database;

public static class DatabaseStartup
{
    public static void AddDatabase(this WebApplicationBuilder builder, string connectionString)
    {
        builder.AddApplicationDatabaseContext(connectionString);

        // Mirror the ID server's Identity options (AspNetIdentitySetup) so validation and stores behave the same.
        builder.Services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3; // passkeys
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Same key ring + application name as the ID server, so password reset tokens generated here
        // validate there. Also gives UserAdmin persistent keys (sessions survive pod restarts/replicas).
        builder.Services.AddDataProtectionAuth(connectionString);

        //builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            // .AddEntityFrameworkStores<ApplicationDbContext>()
            //.AddRoleManager<RoleManager<IdentityRole>>();
    }
}