using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        var configEmail = configuration.GetSection("Email");
        services.Configure<EmailSettings>(configEmail);
        
        // var DbPath = Path.Join(@"C:/temp", "aspnetauth.db");
        // var connectionString = $"Data Source={DbPath}";
        var connectionString = configuration.GetAuthConnectionString();

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddDataProtectionAuth(connectionString);

        services.AddDatabaseDeveloperPageExceptionFilter();

        //var authBuilder = services.AddAuthentication();
        //services.AddAuthorization(options =>
        services.AddAuthentication()
            .AddGoogle(google => {
                google.ClientId = googleSettings.ClientId ?? throw new ApplicationException("missing google ClientId");
                google.ClientSecret = googleSettings.ClientSecret ?? throw new ApplicationException("missing google ClientSecret");
                google.CallbackPath = "/signin-google";
            });

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