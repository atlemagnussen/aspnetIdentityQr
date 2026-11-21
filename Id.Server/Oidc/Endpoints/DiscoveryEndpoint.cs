using Id.Lib.Data;
using Id.Lib.Oidc;
using Id.Server.Oidc.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Id.Server.Oidc.Endpoints;

/// <summary>Discovery document and JWKS. Advertises only what this server actually implements.</summary>
public static class DiscoveryEndpoint
{
    public static async Task<IResult> DiscoveryAsync(HttpContext http, IdDbContext db, SigningKeyStore keys, IOptions<OidcServerOptions> options)
    {
        var issuer = OidcIssuer.Get(http, options.Value);

        var identityResources = await db.IdentityResources.AsNoTracking()
            .Where(r => r.Enabled && r.ShowInDiscoveryDocument)
            .Select(r => new { r.Name, Claims = r.UserClaims.Select(c => c.Type).ToList() })
            .ToListAsync();
        var apiScopes = await db.ApiScopes.AsNoTracking()
            .Where(s => s.Enabled && s.ShowInDiscoveryDocument)
            .Select(s => s.Name).ToListAsync();

        var algorithms = (await keys.GetValidationKeysAsync()).Select(k => k.Algorithm).Distinct().ToArray();

        var doc = new Dictionary<string, object>
        {
            ["issuer"] = issuer,
            ["jwks_uri"] = issuer + OidcPaths.Jwks,
            ["authorization_endpoint"] = issuer + OidcPaths.Authorize,
            ["token_endpoint"] = issuer + OidcPaths.Token,
            ["userinfo_endpoint"] = issuer + OidcPaths.UserInfo,
            ["end_session_endpoint"] = issuer + OidcPaths.EndSession,
            ["scopes_supported"] = identityResources.Select(r => r.Name).Concat(apiScopes).Append(StandardScopes.OfflineAccess).Distinct().ToArray(),
            ["claims_supported"] = identityResources.SelectMany(r => r.Claims).Prepend(JwtClaimTypes.Subject).Distinct().ToArray(),
            ["grant_types_supported"] = new[] { GrantTypes.AuthorizationCode, GrantTypes.RefreshToken },
            ["response_types_supported"] = new[] { "code" },
            ["response_modes_supported"] = new[] { "query", "form_post" },
            ["token_endpoint_auth_methods_supported"] = new[] { "client_secret_basic", "client_secret_post" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = algorithms,
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["request_parameter_supported"] = false,
            ["request_uri_parameter_supported"] = false,
            ["authorization_response_iss_parameter_supported"] = true,
            ["frontchannel_logout_supported"] = false,
            ["backchannel_logout_supported"] = false,
        };
        return Results.Json(doc);
    }

    public static async Task<IResult> JwksAsync(SigningKeyStore keys)
    {
        var jwks = (await keys.GetValidationKeysAsync()).Select(SigningKeyStore.PublicJwk).ToArray();
        return Results.Json(new { keys = jwks });
    }
}
