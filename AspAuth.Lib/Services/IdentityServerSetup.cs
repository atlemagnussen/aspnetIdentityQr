using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AspAuth.Lib.Services;

public static class IdentityServerSetup
{
    public static void ConfigureIdentityServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        var configuration = builder.Configuration;

        var migrationsAssembly = typeof(IdentityServerSetup).Assembly.GetName().Name;
        string connectionString = configuration.GetAuthConnectionString();

        builder.Services.AddIdentityServer()
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddAspNetIdentity<IdentityUser>();

        var authenticationBuilder = builder.Services.AddAuthentication();

        // authenticationBuilder.AddOpenIdConnect("oidc", "Sign-in with oidc", options =>
        // {
        //     options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        //     options.SignOutScheme = IdentityServerConstants.SignoutScheme;
        //     options.SaveTokens = true;
        
        //     options.Authority = "https://demo.duendesoftware.com";
        //     options.ClientId = "interactive.confidential";
        //     options.ClientSecret = "secret";
        //     options.ResponseType = "code";
        
        //     options.TokenValidationParameters = new TokenValidationParameters
        //     {
        //         NameClaimType = "name",
        //         RoleClaimType = "role"
        //     };
        // });
    }
    public static void InitializeDatabase(this IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope())
        {
            serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

            var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();
            if (!context.Clients.Any())
            {
                foreach (var client in Config.Clients)
                {
                    context.Clients.Add(client.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.IdentityResources.Any())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    context.IdentityResources.Add(resource.ToEntity());
                }
                context.SaveChanges();
            }

            if (!context.ApiScopes.Any())
            {
                foreach (var apiScope in Config.ApiScopes)
                {
                    context.ApiScopes.Add(apiScope.ToEntity());
                }
                context.SaveChanges();
            }
        }
    }
}