using AspAuth.Lib.Data;
using AspAuth.Lib.Services;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using UserAdmin.Persistence.Database;

namespace UserAdmin.Persistence;

public static class PersistenceStartup
{
    public static void AddPersistence(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetAuthConnectionString();
        builder.AddDatabase(connectionString);

        builder.AddConfigurationDb(connectionString);
    }

    private static void AddConfigurationDb(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddSingleton(new ConfigurationStoreOptions());
        builder.Services.AddDbContext<ConfigurationDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString);
        });
    }
}