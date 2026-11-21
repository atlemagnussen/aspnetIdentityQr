namespace Id.Lib.Entities;

// Identity resources (scopes that map to user claims), API scopes and API resources (audiences),
// and dynamic external identity providers. Tables are the ones Duende created.

public class IdentityResource
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public bool Emphasize { get; set; }
    public bool ShowInDiscoveryDocument { get; set; } = true;
    public List<IdentityResourceClaim> UserClaims { get; set; } = [];
    public List<IdentityResourceProperty> Properties { get; set; } = [];
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public bool NonEditable { get; set; }
}

public class IdentityResourceClaim
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public int IdentityResourceId { get; set; }
    public IdentityResource IdentityResource { get; set; } = null!;
}

public class IdentityResourceProperty
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public int IdentityResourceId { get; set; }
    public IdentityResource IdentityResource { get; set; } = null!;
}

public class ApiScope
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public bool Emphasize { get; set; }
    public bool ShowInDiscoveryDocument { get; set; } = true;
    public List<ApiScopeClaim> UserClaims { get; set; } = [];
    public List<ApiScopeProperty> Properties { get; set; } = [];
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public DateTime? LastAccessed { get; set; }
    public bool NonEditable { get; set; }
}

public class ApiScopeClaim
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public int ScopeId { get; set; }
    public ApiScope Scope { get; set; } = null!;
}

public class ApiScopeProperty
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public int ScopeId { get; set; }
    public ApiScope Scope { get; set; } = null!;
}

public class ApiResource
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? AllowedAccessTokenSigningAlgorithms { get; set; }
    public bool ShowInDiscoveryDocument { get; set; } = true;
    public bool RequireResourceIndicator { get; set; }
    public List<ApiResourceSecret> Secrets { get; set; } = [];
    public List<ApiResourceScope> Scopes { get; set; } = [];
    public List<ApiResourceClaim> UserClaims { get; set; } = [];
    public List<ApiResourceProperty> Properties { get; set; } = [];
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public DateTime? LastAccessed { get; set; }
    public bool NonEditable { get; set; }
}

public abstract class ApiResourceChild
{
    public int Id { get; set; }
    public int ApiResourceId { get; set; }
    public ApiResource ApiResource { get; set; } = null!;
}

public class ApiResourceScope : ApiResourceChild
{
    public string Scope { get; set; } = "";
}

public class ApiResourceClaim : ApiResourceChild
{
    public string Type { get; set; } = "";
}

public class ApiResourceProperty : ApiResourceChild
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ApiResourceSecret : ApiResourceChild
{
    public string? Description { get; set; }
    public string Value { get; set; } = "";
    public DateTime? Expiration { get; set; }
    public string Type { get; set; } = SecretTypes.SharedSecret;
    public DateTime Created { get; set; } = DateTime.UtcNow;
}

/// <summary>Dynamic external provider (Duende "dynamic providers"). Stored, not used by the ID server.</summary>
public class IdentityProvider
{
    public int Id { get; set; }
    public string Scheme { get; set; } = "";
    public string? DisplayName { get; set; }
    public bool Enabled { get; set; } = true;
    public string Type { get; set; } = "";
    /// <summary>JSON dictionary of provider-specific settings</summary>
    public string? Properties { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public DateTime? LastAccessed { get; set; }
    public bool NonEditable { get; set; }
}
