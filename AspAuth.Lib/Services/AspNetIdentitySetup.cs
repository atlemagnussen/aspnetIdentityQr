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
        var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:AuthDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ApplicationException("no connection string!!!");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        services.AddDataProtectionAuth(connectionString);

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        //var authBuilder = services.AddAuthentication();
        //services.AddAuthorization(options =>
        services.AddAuthentication()
            .AddGoogle(google => {
                google.ClientId = googleSettings.ClientId ?? throw new ApplicationException("missing google ClientId");
                google.ClientSecret = googleSettings.ClientSecret ?? throw new ApplicationException("missing google ClientSecret");
                google.CallbackPath = "/signin-google";
            });

        builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireNonAlphanumeric = false;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>();


        services.AddTransient<IEmailSender<IdentityUser>, AuthEmailSender>();

        builder.Services.Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"));
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