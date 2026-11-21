namespace Id.Lib.Oidc;

/// <summary>Raw JWT claim names (we never map inbound claims to the long .NET URIs).</summary>
public static class JwtClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string EmailVerified = "email_verified";
    public const string Role = "role";
    public const string FullName = "fullname";
    public const string SessionId = "sid";
    public const string AuthenticationTime = "auth_time";
    public const string AuthenticationMethod = "amr";
    public const string IdentityProvider = "idp";
    public const string ClientId = "client_id";
    public const string Scope = "scope";
    public const string JwtId = "jti";
    public const string Nonce = "nonce";
}

public static class StandardScopes
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";
    public const string OfflineAccess = "offline_access";
}

public static class GrantTypes
{
    public const string AuthorizationCode = "authorization_code";
    public const string ClientCredentials = "client_credentials";
    public const string RefreshToken = "refresh_token";
    public const string DeviceCode = "urn:ietf:params:oauth:grant-type:device_code";
    public const string Ciba = "urn:openid:params:grant-type:ciba";
    public const string TokenExchange = "urn:ietf:params:oauth:grant-type:token-exchange";
    public const string Implicit = "implicit";
    public const string Hybrid = "hybrid";
    public const string Password = "password";

    /// <summary>
    /// Grant type rules kept from Duende, so existing client rows stay valid: no spaces, no duplicates,
    /// and implicit, authorization_code and hybrid exclude each other. "refresh_token" is not a client
    /// grant type (refresh tokens come from AllowOfflineAccess).
    /// </summary>
    public static void Validate(IReadOnlyCollection<string> grantTypes)
    {
        if (grantTypes.Any(g => g.Contains(' ')))
            throw new InvalidOperationException("Grant types can't contain spaces.");
        if (grantTypes.Distinct().Count() != grantTypes.Count)
            throw new InvalidOperationException("Grant types list contains duplicates.");
        if (grantTypes.Contains(RefreshToken))
            throw new InvalidOperationException("'refresh_token' isn't a client grant type; use 'Allow offline access'.");

        string[] exclusive = [Implicit, AuthorizationCode, Hybrid];
        var chosen = exclusive.Where(grantTypes.Contains).ToList();
        if (chosen.Count > 1)
            throw new InvalidOperationException($"Grant types {string.Join(" and ", chosen)} can't be combined.");
    }
}
