namespace Id.Lib.Entities;

// OAuth/OIDC clients. Tables and columns are the ones Duende IdentityServer created; the database
// was migrated in place, so names, lengths and nullability must not change. Defaults are the CLR
// defaults Duende used (the columns have no DB defaults).

public class Client
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string ClientId { get; set; } = "";
    public string ProtocolType { get; set; } = ProtocolTypes.OpenIdConnect;
    public List<ClientSecret> ClientSecrets { get; set; } = [];
    public bool RequireClientSecret { get; set; } = true;
    public string? ClientName { get; set; }
    public string? Description { get; set; }
    public string? ClientUri { get; set; }
    public string? LogoUri { get; set; }
    public bool RequireConsent { get; set; }
    public bool AllowRememberConsent { get; set; } = true;
    public bool AlwaysIncludeUserClaimsInIdToken { get; set; }
    public List<ClientGrantType> AllowedGrantTypes { get; set; } = [];
    public bool RequirePkce { get; set; } = true;
    public bool AllowPlainTextPkce { get; set; }
    public bool RequireRequestObject { get; set; }
    public bool AllowAccessTokensViaBrowser { get; set; }
    public bool RequireDPoP { get; set; }
    /// <summary>Duende DPoPTokenExpirationValidationMode flags. Stored, not used.</summary>
    public int DPoPValidationMode { get; set; }
    public TimeSpan DPoPClockSkew { get; set; } = TimeSpan.FromMinutes(5);
    public List<ClientRedirectUri> RedirectUris { get; set; } = [];
    public List<ClientPostLogoutRedirectUri> PostLogoutRedirectUris { get; set; } = [];
    public string? FrontChannelLogoutUri { get; set; }
    public bool FrontChannelLogoutSessionRequired { get; set; } = true;
    public string? BackChannelLogoutUri { get; set; }
    public bool BackChannelLogoutSessionRequired { get; set; } = true;
    public bool AllowOfflineAccess { get; set; }
    public List<ClientScope> AllowedScopes { get; set; } = [];
    /// <summary>Seconds</summary>
    public int IdentityTokenLifetime { get; set; } = 300;
    /// <summary>Comma separated; empty means server default</summary>
    public string? AllowedIdentityTokenSigningAlgorithms { get; set; }
    /// <summary>Seconds</summary>
    public int AccessTokenLifetime { get; set; } = 3600;
    /// <summary>Seconds</summary>
    public int AuthorizationCodeLifetime { get; set; } = 300;
    public int? ConsentLifetime { get; set; }
    /// <summary>Seconds. 0 = no absolute limit (sliding expiration only).</summary>
    public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;
    /// <summary>Seconds</summary>
    public int SlidingRefreshTokenLifetime { get; set; } = 1296000;
    public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.OneTimeOnly;
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }
    public TokenExpiration RefreshTokenExpiration { get; set; } = TokenExpiration.Absolute;
    public AccessTokenType AccessTokenType { get; set; } = AccessTokenType.Jwt;
    public bool EnableLocalLogin { get; set; } = true;
    public List<ClientIdPRestriction> IdentityProviderRestrictions { get; set; } = [];
    public bool IncludeJwtId { get; set; } = true;
    public List<ClientClaim> Claims { get; set; } = [];
    public bool AlwaysSendClientClaims { get; set; }
    public string? ClientClaimsPrefix { get; set; } = "client_";
    public string? PairWiseSubjectSalt { get; set; }
    public List<ClientCorsOrigin> AllowedCorsOrigins { get; set; } = [];
    public List<ClientProperty> Properties { get; set; } = [];
    public int? UserSsoLifetime { get; set; }
    public string? UserCodeType { get; set; }
    public int DeviceCodeLifetime { get; set; } = 300;
    public int? CibaLifetime { get; set; }
    public int? PollingInterval { get; set; }
    public bool? CoordinateLifetimeWithUserSession { get; set; }
    public bool RequirePushedAuthorization { get; set; }
    public int? PushedAuthorizationLifetime { get; set; }
    public string? InitiateLoginUri { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public DateTime? LastAccessed { get; set; }
    public bool NonEditable { get; set; }
}

public abstract class ClientChild
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
}

public class ClientGrantType : ClientChild
{
    public string GrantType { get; set; } = "";
}

public class ClientScope : ClientChild
{
    public string Scope { get; set; } = "";
}

public class ClientRedirectUri : ClientChild
{
    public string RedirectUri { get; set; } = "";
}

public class ClientPostLogoutRedirectUri : ClientChild
{
    public string PostLogoutRedirectUri { get; set; } = "";
}

public class ClientCorsOrigin : ClientChild
{
    public string Origin { get; set; } = "";
}

public class ClientIdPRestriction : ClientChild
{
    public string Provider { get; set; } = "";
}

public class ClientClaim : ClientChild
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ClientProperty : ClientChild
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ClientSecret : ClientChild
{
    public string? Description { get; set; }
    /// <summary>For <see cref="SecretTypes.SharedSecret"/>: base64(SHA-256(secret)), see <c>Hashing.Sha256</c>.</summary>
    public string Value { get; set; } = "";
    public DateTime? Expiration { get; set; }
    public string Type { get; set; } = SecretTypes.SharedSecret;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

// Enum values match Duende's, because the ints are already stored in the database.

public enum AccessTokenType
{
    Jwt = 0,
    Reference = 1,
}

public enum TokenUsage
{
    ReUse = 0,
    OneTimeOnly = 1,
}

public enum TokenExpiration
{
    Sliding = 0,
    Absolute = 1,
}

public static class ProtocolTypes
{
    public const string OpenIdConnect = "oidc";
}

public static class SecretTypes
{
    public const string SharedSecret = "SharedSecret";
}
