using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AspAuth.Lib.Services;

public static class AspNetIdentitySetup
{
    public static void ConfigureAspNetIdentity(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var configGoogle = configuration.GetSection("Authentication:Google");
        var googleSettings = configGoogle.Get<AuthenticationClient>();
        if (googleSettings is null)
            throw new ApplicationException("missing Google settings");

        var configEntraId = configuration.GetSection("Authentication:EntraId");
        var entraSettings = configEntraId.Get<AuthenticationClient>();
        if (entraSettings is null)
            throw new ApplicationException("missing Entra settings");

        var configEmail = configuration.GetSection("Email");
        services.Configure<EmailSettings>(configEmail);
        
        // var DbPath = Path.Join(@"C:/temp", "aspnetauth.db");
        // var connectionString = $"Data Source={DbPath}";
        var connectionString = configuration.GetAuthConnectionString();

        services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString, o =>
            {
                o.ConfigureDataSource(ds => ds.EnableDynamicJson());
            });
        });

        services.AddDataProtectionAuth(connectionString);

        //services.AddDatabaseDeveloperPageExceptionFilter();

        //var authBuilder = services.AddAuthentication();
        //services.AddAuthorization(options =>
        services.AddAuthentication()
            .AddGoogle(google =>
            {
                google.ClientId = googleSettings.ClientId ?? throw new ApplicationException("missing Google ClientId");
                google.ClientSecret = googleSettings.ClientSecret ?? throw new ApplicationException("missing google ClientSecret");
                google.CallbackPath = "/signin-google";
            })
            .AddMicrosoftAccount(ms =>
            {
                ms.ClientId = entraSettings.ClientId ?? throw new ApplicationException("missing Entra ClientId");
                ms.CallbackPath = entraSettings.CallbackPath ?? "/signin-oidc";
            });
            // .AddOpenIdConnect("EntraId", "EntraId", options => {
            //     options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
            //     options.SignOutScheme = IdentityServerConstants.SignoutScheme;
            //     options.Authority = "https://login.microsoftonline.com/common";
            //     options.ClientId = entraSettings.ClientId;
            //     options.ResponseType = "id_token";
            //     options.CallbackPath = entraSettings.CallbackPath ?? "/signin-oidc";
            //     options.SignedOutCallbackPath = entraSettings.SignedOutCallbackPath ?? "/signout-callback-oidc";
            //     options.RemoteAuthenticationTimeout = TimeSpan.FromHours(1);
            //     options.TokenValidationParameters = new TokenValidationParameters
            //     {
            //         NameClaimType = "name",
            //         RoleClaimType = "role",
            //         ValidateIssuer = false
            //     };
            // });

        services.AddDefaultIdentity<IdentityUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireNonAlphanumeric = false;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>();


        services.AddTransient<IEmailSender<IdentityUser>, AuthEmailSender>();

        services.Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"));
    }

    public static void AddDataProtectionAuth(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DataProtectionContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddDataProtection()
            .PersistKeysToDbContext<DataProtectionContext>()
            .SetApplicationName("aspauth");
    }
}