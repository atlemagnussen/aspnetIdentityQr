using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspAuth.Lib.Services;

public static class CommonSetup
{
    public static string GetAuthConnectionString(this IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("ConnectionStrings:AuthDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ApplicationException("no connection string!!!");
        return connectionString;
    }

    public static void LoadServices(this IServiceCollection services)
    {
        services.AddScoped<CryptoKeyService>();
    }
}