using Id.Lib.Oidc;
using Id.Server.Oidc.Clients;
using Id.Server.Oidc.Resources;
using Id.Server.Oidc.Tokens;
using Id.Server.Oidc.Users;
using Microsoft.Extensions.Options;

namespace Id.Server.Oidc.Endpoints;

/// <summary>UserInfo (OIDC Core 5.3): bearer access token with the openid scope.</summary>
public static class UserInfoEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext http,
        TokenValidator validator,
        ClientStore clients,
        ResourceStore resources,
        UserClaimsService users,
        IOptions<OidcServerOptions> options)
    {
        var token = await ReadBearerTokenAsync(http.Request);
        if (token is null)
            return Challenge(http, null);

        var identity = await validator.ValidateAccessTokenAsync(OidcIssuer.Get(http, options.Value), token);
        if (identity is null)
            return Challenge(http, OAuthErrors.InvalidToken);

        var scopes = identity.FindAll(JwtClaimTypes.Scope).Select(c => c.Value).ToList();
        if (!scopes.Contains(StandardScopes.OpenId))
            return Challenge(http, OAuthErrors.InsufficientScope, StatusCodes.Status403Forbidden);

        var sub = identity.FindFirst(JwtClaimTypes.Subject)?.Value;
        var client = await clients.FindEnabledAsync(identity.FindFirst(JwtClaimTypes.ClientId)?.Value);
        var user = sub is null ? null : await users.FindActiveAsync(sub);
        if (client is null || user is null)
            return Challenge(http, OAuthErrors.InvalidToken);

        // Resolve against the client's current settings: a scope removed from the client stops releasing claims.
        var (resolved, _) = await resources.ResolveAsync(client,
            scopes.Where(s => client.AllowedScopes.Any(a => a.Scope == s) || s == StandardScopes.OfflineAccess && client.AllowOfflineAccess));
        var claimTypes = resolved?.IdentityClaimTypes ?? [];

        var body = new Dictionary<string, object> { [JwtClaimTypes.Subject] = sub! };
        foreach (var group in UserClaimsService.ForIdentity(user, claimTypes).GroupBy(c => c.Type))
        {
            var values = group.Select(c => c.ValueType == System.Security.Claims.ClaimValueTypes.Boolean && bool.TryParse(c.Value, out var b)
                ? (object)b : c.Value).Distinct().ToArray();
            body[group.Key] = values.Length == 1 ? values[0] : values;
        }
        return OAuthResults.Json(http, body);
    }

    /// <summary>RFC 6750: Authorization header, or form body on POST. Never the query string.</summary>
    private static async Task<string?> ReadBearerTokenAsync(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header["Bearer ".Length..].Trim();
        if (HttpMethods.IsPost(request.Method) && request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            var token = form["access_token"].ToString();
            return token.Length > 0 ? token : null;
        }
        return null;
    }

    private static IResult Challenge(HttpContext http, string? error, int status = StatusCodes.Status401Unauthorized)
    {
        http.Response.Headers.WWWAuthenticate = error is null ? "Bearer" : $"Bearer error=\"{error}\"";
        OAuthResults.NoStore(http.Response);
        return Results.StatusCode(status);
    }
}
