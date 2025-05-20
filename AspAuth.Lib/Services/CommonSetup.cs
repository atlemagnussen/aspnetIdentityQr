using Microsoft.Extensions.Configuration;

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
}