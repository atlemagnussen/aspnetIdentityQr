using AspAuth.Local.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Local;

public static class HostingExtensions
{
    public static void AddDataProtection(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DataProtectionContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionContext>()
            .SetApplicationName("digiLean");
    }
    public static WebApplication SetPublicUrl(this WebApplication app, IConfiguration configuration)
    {
        if (app.Environment.IsDevelopment())
            return app;

        var publicUrl = configuration.GetValue<string>("Authentication:PublicUrl");

        if (string.IsNullOrEmpty(publicUrl))
            return app;

        var AuthServerUri = new Uri(publicUrl);
        app.Use(async (ctx, next) =>
        {
            // for auth server public url from frontdoor ie.
            ctx.Request.Scheme = AuthServerUri.Scheme;
            ctx.Request.Host = new HostString(AuthServerUri.Host);

            await next();
        });

        return app;
    }
}