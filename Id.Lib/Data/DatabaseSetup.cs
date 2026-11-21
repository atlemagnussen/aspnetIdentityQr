using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Id.Lib.Data;

public static class DatabaseSetup
{
    /// <summary>Data Protection application name. Shared by the ID server and the admin app.</summary>
    public const string DataProtectionAppName = "aspauth";

    public static string GetAuthConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ApplicationException("Missing connection string ConnectionStrings:AuthDb");
        return connectionString;
    }

    public static IServiceCollection AddIdDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IdDbContext>(options => UseIdDatabase(options, connectionString));
        return services;
    }

    public static DbContextOptionsBuilder UseIdDatabase(DbContextOptionsBuilder options, string connectionString) =>
        // Dynamic JSON: CryptoSigningKey.Key (JsonWebKey) is stored as jsonb via System.Text.Json.
        options.UseNpgsql(connectionString, o => o.ConfigureDataSource(ds => ds.EnableDynamicJson()));

    /// <summary>
    /// Shared key ring (same table + app name in both apps). Tokens the admin app generates (password
    /// reset links) validate on the ID server. Give each app's auth cookie its own scheme name.
    /// </summary>
    public static IServiceCollection AddIdDataProtection(this IServiceCollection services)
    {
        services.AddDataProtection()
            .PersistKeysToDbContext<IdDbContext>()
            .SetApplicationName(DataProtectionAppName);
        return services;
    }

    /// <summary>
    /// Options for an <see cref="IdDbContext"/> created outside an app (design-time factory, tests).
    /// Identity reads its schema version from IdentityOptions in the application service provider,
    /// so give it a minimal one with the same options the apps use.
    /// </summary>
    public static DbContextOptions<IdDbContext> StandaloneOptions(string connectionString)
    {
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(ConfigureIdentityOptions);
        var builder = new DbContextOptionsBuilder<IdDbContext>()
            .UseApplicationServiceProvider(services.BuildServiceProvider());
        UseIdDatabase(builder, connectionString);
        return builder.Options;
    }

    /// <summary>Identity options both apps must agree on, so validation and stores behave the same.</summary>
    public static void ConfigureIdentityOptions(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3; // passkeys
    }
}
