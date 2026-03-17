using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspAuth.Lib.Services;

public static class IdentityServerSetup
{
    public static void ConfigureIdentityServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        var configuration = builder.Configuration;

        var migrationsAssembly = typeof(IdentityServerSetup).Assembly.GetName().Name;
        string connectionString = configuration.GetAuthConnectionString();

        var idsvrBuilder = builder.Services.AddIdentityServer(idopts =>
        {
            idopts.KeyManagement.Enabled = false;
            idopts.EmitStaticAudienceClaim = true;
            idopts.UserInteraction.LoginUrl = "/Identity/Account/Login";
            idopts.UserInteraction.ErrorUrl = "/Error";
        })
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
            .AddAspNetIdentity<ApplicationUser>()
            .AddProfileService<CustomProfileService>();


        //builder.Services.AddKeysFromDb(idsvrBuilder);

        var authenticationBuilder = builder.Services.AddAuthentication();
    }

    private static void AddKeysFromDb(this IServiceCollection services, IIdentityServerBuilder builder)
    {
        //idsvrBuilder.AddSigningCredential(key, SecurityAlgorithms.RsaSha256);
    }

    public static void CreateNewKey(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();
        var keyService = serviceScope.ServiceProvider.GetRequiredService<CryptoKeyService>();
        keyService.CreateAndSaveNewKey();
    }

    public static void MigrateDb(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
    }
    public static void InitializeDatabase(this IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>()!.CreateScope();

        serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
    }
}