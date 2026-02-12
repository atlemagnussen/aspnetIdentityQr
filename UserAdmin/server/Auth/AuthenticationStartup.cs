using Microsoft.AspNetCore.Identity;

namespace UserAdmin.Auth;

public static class AuthenticationStartup
{
    public static void AddAuthentication(this WebApplicationBuilder builder, string authServer, string authClient)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            //options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            //options.DefaultChallengeScheme = IdentityConstants.ExternalScheme; 
            //options.DefaultScheme = "Cookies"; // The default authentication scheme for interactive users
            options.DefaultChallengeScheme = "oidc"; // The scheme used to challenge unauthenticated users
        })
        .AddCookie(IdentityConstants.ApplicationScheme, options =>
        {
            options.AccessDeniedPath = "/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14); 
            options.SlidingExpiration = true;
        })
        .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = authServer;
            options.ClientId = authClient;
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.Scope.Add("openid");
            options.Scope.Add("profile");

            options.CallbackPath = "/signin-oidc";
            options.MapInboundClaims = false;

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events.OnRemoteFailure = context =>
            {
                context.Response.Redirect("/Error?message=" + context.Failure?.Message);
                context.HandleResponse();
                return Task.CompletedTask;
            };
        });

        //builder.Services.AddHttpContextAccessor();
    }
}