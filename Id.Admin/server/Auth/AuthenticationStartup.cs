using Id.Lib.Entities;
using Id.Lib.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Id.Admin.Auth;

public static class AuthenticationStartup
{
    /// <summary>
    /// Own cookie scheme name (not Identity.Application): UserAdmin shares the Data Protection key ring with
    /// the ID server (needed for password reset tokens), and the scheme name is part of the cookie protector
    /// purpose. A distinct name means an ID server session cookie can never be accepted here.
    /// </summary>
    public const string CookieScheme = "UserAdmin";

    /// <summary>Challenges API calls with the cookie handler (plain 401/403), pages with OIDC (redirect to the ID server).</summary>
    private const string ChallengeScheme = "UserAdmin.Challenge";

    private static bool IsApi(HttpRequest request) => request.Path.StartsWithSegments("/api");

    public static void AddAuthentication(this WebApplicationBuilder builder, AdminOptions adminOptions)
    {
        var authServer = adminOptions.AuthServer;
        var authClient = adminOptions.AuthClient;

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieScheme;
            //options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            //options.DefaultChallengeScheme = IdentityConstants.ExternalScheme; 
            //options.DefaultScheme = "Cookies"; // The default authentication scheme for interactive users
            options.DefaultChallengeScheme = ChallengeScheme;
        })
        .AddPolicyScheme(ChallengeScheme, null, options =>
        {
            options.ForwardDefaultSelector = context => IsApi(context.Request) ? CookieScheme : "oidc";
        })
        .AddCookie(CookieScheme, options =>
        {
            options.Cookie.Name = ".UserAdmin.Auth";
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.AccessDeniedPath = "/AccessDenied";
            options.ExpireTimeSpan = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;

            // API calls get status codes, not HTML redirects
            options.Events.OnRedirectToLogin = context =>
            {
                if (IsApi(context.Request))
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (IsApi(context.Request))
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                else
                    context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
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
            options.RequireHttpsMetadata = false;

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