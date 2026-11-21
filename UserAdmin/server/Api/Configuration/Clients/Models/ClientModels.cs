using Duende.IdentityServer.Models;

namespace UserAdmin.Api.Configuration.Clients.Models;

public class ClientSummaryDto
{
    public required string ClientId { get; set; }
    public string? ClientName { get; set; }
    public required bool Enabled { get; set; }
    public required List<string> AllowedGrantTypes { get; set; }
    public required bool RequireClientSecret { get; set; }
    public required int SecretCount { get; set; }
    public required int RedirectUriCount { get; set; }
    public required int AllowedScopeCount { get; set; }
    public DateTime? LastAccessed { get; set; }
    /// <summary>The client UserAdmin itself signs in with</summary>
    public required bool IsProtected { get; set; }
}

/// <summary>
/// Editable client settings. Used for read, create and full update (PUT replaces all collections).
/// Secrets are managed separately; read-only fields are ignored on write.
/// </summary>
public class ClientDto
{
    public required string ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? Description { get; set; }
    public string? ClientUri { get; set; }
    public string? LogoUri { get; set; }
    public required bool Enabled { get; set; }
    public required string ProtocolType { get; set; }

    // Authentication / flow
    public required List<string> AllowedGrantTypes { get; set; }
    public required bool RequireClientSecret { get; set; }
    public required bool RequirePkce { get; set; }
    public required bool AllowPlainTextPkce { get; set; }
    public required bool RequireRequestObject { get; set; }
    public required bool RequireDPoP { get; set; }
    public required bool RequirePushedAuthorization { get; set; }
    public required bool AllowAccessTokensViaBrowser { get; set; }
    public required bool AllowOfflineAccess { get; set; }
    public required bool EnableLocalLogin { get; set; }
    public required List<string> IdentityProviderRestrictions { get; set; }

    // Consent
    public required bool RequireConsent { get; set; }
    public required bool AllowRememberConsent { get; set; }
    public int? ConsentLifetime { get; set; }

    // URLs
    public required List<string> RedirectUris { get; set; }
    public required List<string> PostLogoutRedirectUris { get; set; }
    public required List<string> AllowedCorsOrigins { get; set; }
    public string? FrontChannelLogoutUri { get; set; }
    public required bool FrontChannelLogoutSessionRequired { get; set; }
    public string? BackChannelLogoutUri { get; set; }
    public required bool BackChannelLogoutSessionRequired { get; set; }
    public string? InitiateLoginUri { get; set; }

    // Scopes
    public required List<string> AllowedScopes { get; set; }

    // Tokens (lifetimes in seconds)
    public required AccessTokenType AccessTokenType { get; set; }
    public required int AccessTokenLifetime { get; set; }
    public required int IdentityTokenLifetime { get; set; }
    public required int AuthorizationCodeLifetime { get; set; }
    public required int AbsoluteRefreshTokenLifetime { get; set; }
    public required int SlidingRefreshTokenLifetime { get; set; }
    public required TokenUsage RefreshTokenUsage { get; set; }
    public required TokenExpiration RefreshTokenExpiration { get; set; }
    public required bool UpdateAccessTokenClaimsOnRefresh { get; set; }
    public required bool IncludeJwtId { get; set; }
    public required bool AlwaysIncludeUserClaimsInIdToken { get; set; }
    public int? UserSsoLifetime { get; set; }
    public required bool CoordinateLifetimeWithUserSession { get; set; }
    public required List<string> AllowedIdentityTokenSigningAlgorithms { get; set; }

    // Client claims / properties
    public required List<ClientClaimDto> Claims { get; set; }
    public required bool AlwaysSendClientClaims { get; set; }
    public string? ClientClaimsPrefix { get; set; }
    public string? PairWiseSubjectSalt { get; set; }
    public required List<ClientPropertyDto> Properties { get; set; }

    // Read-only info
    public DateTime? Created { get; set; }
    public DateTime? Updated { get; set; }
    public DateTime? LastAccessed { get; set; }
    public bool NonEditable { get; set; }
    public bool IsProtected { get; set; }
}

public class ClientClaimDto
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}

public class ClientPropertyDto
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public class ClientSecretDto
{
    public required int Id { get; set; }
    public string? Description { get; set; }
    public required string Type { get; set; }
    public DateTime? Expiration { get; set; }
    public required DateTime Created { get; set; }
}

public class ClientSecretCreateDto
{
    public string? Description { get; set; }
    public DateTime? Expiration { get; set; }
}

public class ClientSecretCreatedDto
{
    public required ClientSecretDto Secret { get; set; }
    /// <summary>Plain-text secret. Only returned once; the database stores a SHA-256 hash.</summary>
    public required string Value { get; set; }
}

public class ScopeDto
{
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
    /// <summary>"identity" (identity resource) or "api" (API scope)</summary>
    public required string Kind { get; set; }
    public required bool Enabled { get; set; }
}
