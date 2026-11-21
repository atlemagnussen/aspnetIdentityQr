using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Id.Lib.Entities;
using Id.Lib.Oidc;
using Id.Server.Oidc.Resources;
using Id.Server.Oidc.Users;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Id.Server.Oidc.Tokens;

/// <summary>Everything needed to mint tokens for one user + client + scope set.</summary>
public sealed record TokenSubject(
    Client Client,
    UserData User,
    ResolvedScopes Scopes,
    string? SessionId,
    long AuthTime,
    string Idp,
    string[] Amr);

/// <summary>
/// Mints JWTs with the same shape as the old server: access tokens typ "at+jwt", the static audience
/// "{issuer}/resources" plus API resource names, scope as an array; id tokens typ "JWT" with at_hash.
/// </summary>
public class TokenIssuer(SigningKeyStore keys, TimeProvider time)
{
    private readonly JsonWebTokenHandler _handler = new();

    public async Task<string> CreateAccessTokenAsync(string issuer, TokenSubject s)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtClaimTypes.ClientId] = s.Client.ClientId,
            [JwtClaimTypes.Subject] = s.User.User.Id,
            [JwtClaimTypes.AuthenticationTime] = s.AuthTime,
            [JwtClaimTypes.IdentityProvider] = s.Idp,
            [JwtClaimTypes.AuthenticationMethod] = s.Amr,
            [JwtClaimTypes.Scope] = s.Scopes.Scopes.ToArray(),
        };
        if (s.SessionId is not null)
            claims[JwtClaimTypes.SessionId] = s.SessionId;
        if (s.Client.IncludeJwtId)
            claims[JwtClaimTypes.JwtId] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        AddClaims(claims, UserClaimsService.ForAccessToken(s.User, s.Scopes.AccessTokenClaimTypes));
        if (s.Client.AlwaysSendClientClaims)
            AddClaims(claims, s.Client.Claims.Select(c => new Claim((s.Client.ClientClaimsPrefix ?? "") + c.Type, c.Value)));

        var audiences = new List<string> { OidcIssuer.StaticAudience(issuer) };
        audiences.AddRange(s.Scopes.ApiResources.Select(r => r.Name).Distinct());

        return await CreateAsync(issuer, audiences, "at+jwt", s.Client.AccessTokenLifetime, claims);
    }

    public async Task<string> CreateIdTokenAsync(string issuer, TokenSubject s, string? nonce, string? accessToken)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtClaimTypes.Subject] = s.User.User.Id,
            [JwtClaimTypes.AuthenticationTime] = s.AuthTime,
            [JwtClaimTypes.IdentityProvider] = s.Idp,
            [JwtClaimTypes.AuthenticationMethod] = s.Amr,
        };
        if (s.SessionId is not null)
            claims[JwtClaimTypes.SessionId] = s.SessionId;
        if (nonce is not null)
            claims[JwtClaimTypes.Nonce] = nonce;

        var key = await keys.GetSigningKeyAsync();
        if (accessToken is not null)
            claims["at_hash"] = LeftHalfHash(accessToken, key.Algorithm);

        // Without AlwaysIncludeUserClaimsInIdToken the client gets user claims from userinfo.
        if (s.Client.AlwaysIncludeUserClaimsInIdToken)
            AddClaims(claims, UserClaimsService.ForIdentity(s.User, s.Scopes.IdentityClaimTypes));

        return await CreateAsync(issuer, [s.Client.ClientId], "JWT", s.Client.IdentityTokenLifetime, claims, key);
    }

    private async Task<string> CreateAsync(string issuer, List<string> audiences, string type, int lifetimeSeconds,
        Dictionary<string, object> claims, SigningKey? key = null)
    {
        key ??= await keys.GetSigningKeyAsync();
        var now = time.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddSeconds(lifetimeSeconds),
            TokenType = type,
            Claims = claims,
            SigningCredentials = key.Credentials,
        };
        // One audience is a string, several an array (both valid per RFC 7519).
        if (audiences.Count == 1) descriptor.Audience = audiences[0];
        else foreach (var a in audiences) descriptor.Audiences.Add(a);

        return _handler.CreateToken(descriptor);
    }

    /// <summary>Single value as a string, repeated types as an array (e.g. several roles).</summary>
    private static void AddClaims(Dictionary<string, object> target, IEnumerable<Claim> claims)
    {
        foreach (var group in claims.GroupBy(c => c.Type))
        {
            if (target.ContainsKey(group.Key))
                continue; // protocol claims win
            var values = group.Select(ClaimValue).Distinct().ToArray();
            target[group.Key] = values.Length == 1 ? values[0] : values;
        }
    }

    private static object ClaimValue(Claim c) =>
        c.ValueType == ClaimValueTypes.Boolean && bool.TryParse(c.Value, out var b) ? b : c.Value;

    /// <summary>OIDC Core 3.1.3.6: left half of the hash of the ASCII token, hash size from the alg.</summary>
    public static string LeftHalfHash(string token, string algorithm)
    {
        var bytes = Encoding.ASCII.GetBytes(token);
        var hash = algorithm[^3..] switch
        {
            "256" => SHA256.HashData(bytes),
            "384" => SHA384.HashData(bytes),
            "512" => SHA512.HashData(bytes),
            _ => throw new NotSupportedException(algorithm),
        };
        return WebEncoders.Base64UrlEncode(hash, 0, hash.Length / 2);
    }
}

/// <summary>Validates tokens this server issued (userinfo, end session).</summary>
public class TokenValidator(SigningKeyStore keys)
{
    private readonly JsonWebTokenHandler _handler = new();

    public async Task<ClaimsIdentity?> ValidateAccessTokenAsync(string issuer, string token) =>
        await ValidateAsync(issuer, token, "at+jwt", validateLifetime: true);

    /// <summary>id_token_hint: our signature and issuer; an expired hint is still a valid hint.</summary>
    public async Task<ClaimsIdentity?> ValidateIdTokenHintAsync(string issuer, string token) =>
        await ValidateAsync(issuer, token, "JWT", validateLifetime: false);

    private async Task<ClaimsIdentity?> ValidateAsync(string issuer, string token, string type, bool validateLifetime)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 8192)
            return null;
        var result = await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidateAudience = false,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidTypes = [type],
            IssuerSigningKeys = (await keys.GetValidationKeysAsync()).Select(k => (SecurityKey)k.Key),
            ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512, SecurityAlgorithms.RsaSha256],
        });
        return result.IsValid ? result.ClaimsIdentity : null;
    }
}
