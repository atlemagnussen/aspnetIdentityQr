using AspAuth.Lib.Models;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Authentication;
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
            options.Scope.Add("roles");

            options.MapInboundClaims = false;

            options.ClaimActions.Clear();
            options.ClaimActions.DeleteClaim("nonce");
            options.ClaimActions.DeleteClaim("at_hash");

            options.ClaimActions.MapJsonKey("role", "role", "role"); 
            options.ClaimActions.MapUniqueJsonKey("name", "name", "name");

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.CallbackPath = "/signin-oidc";

            options.Events.OnRemoteFailure = context =>
            {
                context.Response.Redirect("/Error?message=" + context.Failure?.Message);
                context.HandleResponse();
                return Task.CompletedTask;
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequiresAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(JwtClaimTypes.Role, UserRoles.Admin.ToString());
            });
        });
        //builder.Services.AddHttpContextAccessor();
    }
}