using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AspAuth.Lib.Services;

public static class CookieSetup {

    public static void ConfigureCookies(this WebApplicationBuilder builder)
    {
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax; // to work with localhost / test environment

            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;

            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true; // slide after half of expirationTime

            options.LoginPath = "/Login";
            options.AccessDeniedPath = "/AccessDenied";
        });

        // External cookie is what we use in authenticate with external provider, MS/Google in this case
        builder.Services.ConfigureExternalCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
    }
}