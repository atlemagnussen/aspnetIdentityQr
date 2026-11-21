using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Id.Lib.Entities;
using Id.Lib.Oidc;
using Id.Server.Oidc.Clients;
using Id.Server.Oidc.Grants;
using Id.Server.Oidc.Resources;
using Id.Server.Oidc.Tokens;
using Id.Server.Oidc.Users;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Id.Server.Oidc.Endpoints;

/// <summary>Token endpoint: authorization_code (with PKCE) and refresh_token grants.</summary>
public static partial class TokenEndpoint
{
    [GeneratedRegex("^[A-Za-z0-9._~-]{43,128}$")]
    private static partial Regex CodeVerifier();

    public sealed record Services(
        ClientAuthenticator Authenticator,
        ResourceStore Resources,
        UserClaimsService Users,
        GrantStore Grants,
        TokenIssuer Issuer,
        TimeProvider Time,
        IOptions<OidcServerOptions> Options,
        ILoggerFactory LoggerFactory);

    public static async Task<IResult> HandleAsync(HttpContext http, [AsParameters] Services s)
    {
        if (!http.Request.HasFormContentType)
            return OAuthResults.Error(http, OAuthErrors.InvalidRequest, "The request must be form encoded.");
        var form = await http.Request.ReadFormAsync();
        if (form.Any(kv => kv.Value.Count > 1))
            return OAuthResults.Error(http, OAuthErrors.InvalidRequest, "Repeated parameters.");

        var auth = await s.Authenticator.AuthenticateAsync(http.Request, form);
        if (auth.Client is null)
        {
            if (auth.Error == OAuthErrors.InvalidClient)
            {
                if (auth.UsedBasic)
                    http.Response.Headers.WWWAuthenticate = "Basic realm=\"token\"";
                return OAuthResults.Error(http, auth.Error, auth.Description, StatusCodes.Status401Unauthorized);
            }
            return OAuthResults.Error(http, auth.Error!, auth.Description);
        }

        var ctx = new Context(http, s, auth.Client, form, OidcIssuer.Get(http, s.Options.Value),
            s.LoggerFactory.CreateLogger(typeof(TokenEndpoint)));

        return form["grant_type"].ToString() switch
        {
            GrantTypes.AuthorizationCode => await AuthorizationCodeAsync(ctx),
            GrantTypes.RefreshToken => await RefreshTokenAsync(ctx),
            "" => OAuthResults.Error(http, OAuthErrors.InvalidRequest, "grant_type is required."),
            _ => OAuthResults.Error(http, OAuthErrors.UnsupportedGrantType),
        };
    }

    private sealed record Context(HttpContext Http, Services S, Client Client, IFormCollection Form, string Issuer, ILogger Log)
    {
        public IResult Error(string error, string? description = null) => OAuthResults.Error(Http, error, description);
        public DateTime Now => S.Time.GetUtcNow().UtcDateTime;
    }

    private static async Task<IResult> AuthorizationCodeAsync(Context c)
    {
        if (!c.Client.AllowedGrantTypes.Any(g => g.GrantType == GrantTypes.AuthorizationCode))
            return c.Error(OAuthErrors.UnauthorizedClient);

        var found = await c.S.Grants.FindAsync<AuthorizationCodeData>(GrantTypesStored.AuthorizationCode, c.Form["code"]);
        if (found is not { } f || f.Data.ClientId != c.Client.ClientId)
            return c.Error(OAuthErrors.InvalidGrant, "Invalid authorization code.");
        var (grant, code) = f;

        if (grant.Expiration is { } exp && exp <= c.Now)
            return c.Error(OAuthErrors.InvalidGrant, "Authorization code expired.");

        if (!await c.S.Grants.TryConsumeAsync(grant.Id))
        {
            // Second use of a code: someone else may hold it. Revoke what the first use produced (RFC 6749 4.1.2).
            var revoked = await c.S.Grants.RevokeAsync(code.ClientId, code.SubjectId, code.SessionId);
            c.Log.LogWarning("Authorization code replay for client {ClientId}, subject {SubjectId}; revoked {Count} refresh tokens",
                code.ClientId, code.SubjectId, revoked);
            return c.Error(OAuthErrors.InvalidGrant, "Authorization code already used.");
        }

        if (c.Form["redirect_uri"].ToString() != code.RedirectUri)
            return c.Error(OAuthErrors.InvalidGrant, "redirect_uri doesn't match the authorization request.");

        var verifier = c.Form["code_verifier"].ToString();
        if (code.CodeChallenge is null)
        {
            // A verifier without a challenge means the challenge was stripped somewhere: PKCE downgrade.
            if (verifier.Length > 0)
                return c.Error(OAuthErrors.InvalidGrant, "code_verifier sent, but the authorization request had no code_challenge.");
        }
        else if (!CodeVerifier().IsMatch(verifier) || !PkceMatches(verifier, code.CodeChallenge))
        {
            return c.Error(OAuthErrors.InvalidGrant, "Invalid code_verifier.");
        }

        var user = await c.S.Users.FindActiveAsync(code.SubjectId);
        if (user is null)
            return c.Error(OAuthErrors.InvalidGrant, "The user is no longer active.");

        var (scopes, invalid) = await c.S.Resources.ResolveAsync(c.Client, code.Scopes);
        if (scopes is null)
            return c.Error(OAuthErrors.InvalidGrant, $"Scopes are no longer allowed: {invalid}");

        var subject = new TokenSubject(c.Client, user, scopes, code.SessionId, code.AuthTime, code.Idp, code.Amr);
        string? refresh = null;
        if (scopes.OfflineAccess)
        {
            var data = new RefreshTokenData(c.Client.ClientId, code.SubjectId, code.SessionId, code.Scopes,
                code.AuthTime, code.Idp, code.Amr, c.Now);
            refresh = await c.S.Grants.CreateAsync(GrantTypesStored.RefreshToken, data, c.Client.ClientId,
                code.SubjectId, code.SessionId, RefreshExpiration(c.Client, data.ChainCreated, c.Now));
        }

        return await TokenResponseAsync(c, subject, code.Nonce, refresh);
    }

    private static async Task<IResult> RefreshTokenAsync(Context c)
    {
        if (!c.Client.AllowOfflineAccess)
            return c.Error(OAuthErrors.UnauthorizedClient);

        var handle = c.Form["refresh_token"].ToString();
        var found = await c.S.Grants.FindAsync<RefreshTokenData>(GrantTypesStored.RefreshToken, handle);
        if (found is not { } f || f.Data.ClientId != c.Client.ClientId)
            return c.Error(OAuthErrors.InvalidGrant, "Invalid refresh token.");
        var (grant, token) = f;

        if ((grant.Expiration is { } exp && exp <= c.Now) || grant.ConsumedTime is not null)
            return c.Error(OAuthErrors.InvalidGrant, "Refresh token expired or already used.");

        // Optional narrowing (RFC 6749 6): never wider than the original grant.
        var requested = AuthorizeEndpoint.SplitScopes(c.Form["scope"]).ToArray();
        if (requested.Length > 0 && requested.Any(r => !token.Scopes.Contains(r)))
            return c.Error(OAuthErrors.InvalidScope, "scope exceeds the original grant.");
        var scopeNames = requested.Length > 0 ? requested : token.Scopes;

        var user = await c.S.Users.FindActiveAsync(token.SubjectId);
        if (user is null)
            return c.Error(OAuthErrors.InvalidGrant, "The user is no longer active.");

        var (scopes, invalid) = await c.S.Resources.ResolveAsync(c.Client, scopeNames);
        if (scopes is null)
            return c.Error(OAuthErrors.InvalidGrant, $"Scopes are no longer allowed: {invalid}");

        string newHandle;
        if (c.Client.RefreshTokenUsage == TokenUsage.OneTimeOnly)
        {
            if (!await c.S.Grants.TryConsumeAsync(grant.Id))
                return c.Error(OAuthErrors.InvalidGrant, "Refresh token already used.");
            newHandle = await c.S.Grants.CreateAsync(GrantTypesStored.RefreshToken, token, c.Client.ClientId,
                token.SubjectId, token.SessionId, RefreshExpiration(c.Client, token.ChainCreated, c.Now));
        }
        else
        {
            newHandle = handle;
            if (c.Client.RefreshTokenExpiration == TokenExpiration.Sliding)
                await c.S.Grants.SetExpirationAsync(grant.Id, RefreshExpiration(c.Client, token.ChainCreated, c.Now));
        }

        // Claims are always read fresh from the database, so role changes reach the next access token.
        var subject = new TokenSubject(c.Client, user, scopes, token.SessionId, token.AuthTime, token.Idp, token.Amr);
        return await TokenResponseAsync(c, subject, nonce: null, newHandle);
    }

    private static async Task<IResult> TokenResponseAsync(Context c, TokenSubject subject, string? nonce, string? refreshToken)
    {
        var accessToken = await c.S.Issuer.CreateAccessTokenAsync(c.Issuer, subject);
        var body = new Dictionary<string, object>
        {
            ["access_token"] = accessToken,
            ["token_type"] = "Bearer",
            ["expires_in"] = c.Client.AccessTokenLifetime,
            ["scope"] = string.Join(' ', subject.Scopes.Scopes),
        };
        if (subject.Scopes.OpenId)
            body["id_token"] = await c.S.Issuer.CreateIdTokenAsync(c.Issuer, subject, nonce, accessToken);
        if (refreshToken is not null)
            body["refresh_token"] = refreshToken;
        return OAuthResults.Json(c.Http, body);
    }

    /// <summary>
    /// Absolute: fixed end, counted from the first token of the chain. Sliding: extends on each use,
    /// capped by the absolute lifetime (0 = no cap).
    /// </summary>
    public static DateTime RefreshExpiration(Client client, DateTime chainCreated, DateTime now)
    {
        var absoluteEnd = client.AbsoluteRefreshTokenLifetime > 0
            ? chainCreated.AddSeconds(client.AbsoluteRefreshTokenLifetime)
            : DateTime.MaxValue;
        if (client.RefreshTokenExpiration == TokenExpiration.Absolute)
            return absoluteEnd == DateTime.MaxValue ? now.AddSeconds(client.SlidingRefreshTokenLifetime) : absoluteEnd;
        var slidingEnd = now.AddSeconds(client.SlidingRefreshTokenLifetime);
        return slidingEnd < absoluteEnd ? slidingEnd : absoluteEnd;
    }

    private static bool PkceMatches(string verifier, string challenge)
    {
        var computed = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(challenge));
    }
}
