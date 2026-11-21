using Id.Server.Oidc.Clients;
using Id.Server.Oidc.Endpoints;
using Id.Server.Oidc.Grants;
using Id.Server.Oidc.Resources;
using Id.Server.Oidc.Tokens;
using Id.Server.Oidc.Users;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Id.Server.Oidc;

public static class OidcSetup
{
    public static void AddOidcServer(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.Configure<OidcServerOptions>(builder.Configuration.GetSection("Oidc"));
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<SigningKeyStore>();
        services.AddSingleton<TokenIssuer>();
        services.AddSingleton<TokenValidator>();
        services.AddScoped<ClientStore>();
        services.AddScoped<ClientAuthenticator>();
        services.AddScoped<ResourceStore>();
        services.AddScoped<UserClaimsService>();
        services.AddScoped<GrantStore>();
        services.AddHostedService<GrantCleanupService>();

        services.AddCors();
        services.AddSingleton<ICorsPolicyProvider, OidcCorsPolicyProvider>();
    }

    public static void MapOidcEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(OidcPaths.Discovery, DiscoveryEndpoint.DiscoveryAsync);
        app.MapGet(OidcPaths.Jwks, DiscoveryEndpoint.JwksAsync);
        app.MapMethods(OidcPaths.Authorize, ["GET", "POST"], AuthorizeEndpoint.HandleAsync).DisableAntiforgery();
        app.MapPost(OidcPaths.Token, TokenEndpoint.HandleAsync).DisableAntiforgery();
        app.MapMethods(OidcPaths.UserInfo, ["GET", "POST"], UserInfoEndpoint.HandleAsync).DisableAntiforgery();
        app.MapMethods(OidcPaths.EndSession, ["GET", "POST"], EndSessionEndpoint.HandleAsync).DisableAntiforgery();
    }
}
