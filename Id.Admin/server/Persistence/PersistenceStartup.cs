using Id.Lib.Data;
using Id.Lib.Entities;
using Microsoft.AspNetCore.Identity;

namespace Id.Admin.Persistence;

public static class PersistenceStartup
{
    public static void AddPersistence(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetAuthConnectionString();
        builder.Services.AddIdDatabase(connectionString);

        builder.Services.AddIdentityCore<ApplicationUser>(DatabaseSetup.ConfigureIdentityOptions)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdDbContext>()
            .AddDefaultTokenProviders();

        // Same key ring + application name as the ID server, so password reset tokens generated here
        // validate there. Also gives this app persistent keys (sessions survive pod restarts/replicas).
        builder.Services.AddIdDataProtection();
    }
}
