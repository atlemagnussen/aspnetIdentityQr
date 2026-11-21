using Id.Lib.Data;
using Id.Lib.Entities;
using Id.Lib.Oidc;
using Id.Server.Models;
using Id.Server.Oidc.Session;
using Microsoft.AspNetCore.Identity;

namespace Id.Server.Services;

public static class IdentitySetup
{
    public static void AddIdentityServerIdentity(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<AuthenticationSettings>(configuration.GetSection("Authentication"));

        var connectionString = configuration.GetAuthConnectionString();
        services.AddIdDatabase(connectionString);
        services.AddIdDataProtection();

        services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                DatabaseSetup.ConfigureIdentityOptions(options);
                // JWT claim names in the Identity cookie. Duende configured the same, so sessions
                // that were signed in before the switch stay valid.
                options.ClaimsIdentity.UserIdClaimType = JwtClaimTypes.Subject;
                options.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
                options.ClaimsIdentity.RoleClaimType = JwtClaimTypes.Role;
                options.ClaimsIdentity.EmailClaimType = JwtClaimTypes.Email;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdDbContext>();

        services.AddTransient<IEmailSender<ApplicationUser>, AuthEmailSender>();

        // Keep the OIDC session claims (sid, auth_time, idp, amr) when the security stamp validator
        // rebuilds the principal.
        services.Configure<SecurityStampValidatorOptions>(o => o.OnRefreshingPrincipal = SessionClaims.CopyOnRefresh);

        AddExternalLogins(builder);
        ConfigureCookies(builder);
        ConfigurePasskeys(builder);
    }

    private static void AddExternalLogins(WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var google = configuration.GetSection("Authentication:Google").Get<AuthenticationClient>()
            ?? throw new ApplicationException("Missing Authentication:Google settings");
        var entra = configuration.GetSection("Authentication:EntraId").Get<AuthenticationClient>()
            ?? throw new ApplicationException("Missing Authentication:EntraId settings");

        builder.Services.AddAuthentication()
            .AddGoogle(o =>
            {
                o.ClientId = google.ClientId ?? throw new ApplicationException("Missing Google ClientId");
                o.ClientSecret = google.ClientSecret ?? throw new ApplicationException("Missing Google ClientSecret");
                o.CallbackPath = "/signin-google";
            })
            .AddMicrosoftAccount(o =>
            {
                o.ClientId = entra.ClientId ?? throw new ApplicationException("Missing Entra ClientId");
                o.ClientSecret = entra.ClientSecret ?? throw new ApplicationException("Missing Entra ClientSecret");
                o.CallbackPath = entra.CallbackPath ?? "/signin-oidc";
            });
    }

    private static void ConfigureCookies(WebApplicationBuilder builder)
    {
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.Events.OnSigningIn = SessionClaims.AddOnSignIn;
        });

        // The external cookie holds the result of a Google/Microsoft login until it's linked to a user.
        builder.Services.ConfigureExternalCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
    }

    private static void ConfigurePasskeys(WebApplicationBuilder builder)
    {
        var isDev = builder.Environment.IsDevelopment();
        builder.Services.Configure<IdentityPasskeyOptions>(options =>
        {
            options.ServerDomain = isDev ? null : "logout.work";
            options.AuthenticatorTimeout = TimeSpan.FromMinutes(3);
            options.UserVerificationRequirement = "required";
            options.ResidentKeyRequirement = "preferred";
            options.ChallengeSize = 64;
            // Attestation statements aren't verified (same as before).
            options.VerifyAttestationStatement = _ => ValueTask.FromResult(true);
        });
    }
}
