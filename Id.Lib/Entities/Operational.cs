using Microsoft.IdentityModel.Tokens;

namespace Id.Lib.Entities;

/// <summary>
/// Short- and long-lived protocol artifacts: authorization codes, refresh tokens, reference tokens, consent.
/// <see cref="Key"/> is a hash of the handle given to the client, never the handle itself.
/// </summary>
public class PersistedGrant
{
    public long Id { get; set; }
    public string? Key { get; set; }
    public string Type { get; set; } = "";
    public string? SubjectId { get; set; }
    public string? SessionId { get; set; }
    public string ClientId { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? Expiration { get; set; }
    public DateTime? ConsumedTime { get; set; }
    public string Data { get; set; } = "";
}

/// <summary>Device flow. Table kept from Duende; not used yet.</summary>
public class DeviceFlowCode
{
    public string UserCode { get; set; } = "";
    public string DeviceCode { get; set; } = "";
    public string? SubjectId { get; set; }
    public string? SessionId { get; set; }
    public string ClientId { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime Expiration { get; set; }
    public string Data { get; set; } = "";
}

/// <summary>Duende automatic key management. Table kept; not used (we sign with <see cref="CryptoSigningKey"/>).</summary>
public class DuendeKey
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public DateTime Created { get; set; }
    public string? Use { get; set; }
    public string Algorithm { get; set; } = "";
    public bool IsX509Certificate { get; set; }
    public bool DataProtected { get; set; }
    public string Data { get; set; } = "";
}

/// <summary>Server-side sessions. Table kept from Duende; not used yet.</summary>
public class ServerSideSession
{
    public long Id { get; set; }
    public string Key { get; set; } = "";
    public string Scheme { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string? SessionId { get; set; }
    public string? DisplayName { get; set; }
    public DateTime Created { get; set; }
    public DateTime Renewed { get; set; }
    public DateTime? Expires { get; set; }
    public string Data { get; set; } = "";
}

/// <summary>PAR (RFC 9126). Table kept from Duende; not used yet.</summary>
public class PushedAuthorizationRequest
{
    public long Id { get; set; }
    public string ReferenceValueHash { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public string Parameters { get; set; } = "";
}

/// <summary>Token signing key. The JWK (including the private part) is stored as jsonb. Table "SigningKeys".</summary>
public class CryptoSigningKey
{
    public int Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public required JsonWebKey Key { get; set; }
}
