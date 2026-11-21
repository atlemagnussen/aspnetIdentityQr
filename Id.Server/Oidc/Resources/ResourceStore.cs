using Id.Lib.Data;
using Id.Lib.Entities;
using Id.Lib.Oidc;
using Microsoft.EntityFrameworkCore;

namespace Id.Server.Oidc.Resources;

/// <summary>What a set of requested scopes resolves to.</summary>
public sealed record ResolvedScopes(
    IReadOnlyList<string> Scopes,
    IReadOnlyList<IdentityResource> IdentityResources,
    IReadOnlyList<ApiScope> ApiScopes,
    IReadOnlyList<ApiResource> ApiResources)
{
    public bool OpenId => Scopes.Contains(StandardScopes.OpenId);
    public bool OfflineAccess => Scopes.Contains(StandardScopes.OfflineAccess);

    /// <summary>User claim types for identity tokens and userinfo.</summary>
    public HashSet<string> IdentityClaimTypes =>
        [.. IdentityResources.SelectMany(r => r.UserClaims).Select(c => c.Type)];

    /// <summary>User claim types for access tokens (from API scopes and API resources).</summary>
    public HashSet<string> AccessTokenClaimTypes =>
        [.. ApiScopes.SelectMany(s => s.UserClaims).Select(c => c.Type)
            .Concat(ApiResources.SelectMany(r => r.UserClaims).Select(c => c.Type))];
}

public class ResourceStore(IdDbContext db)
{
    /// <summary>
    /// Every requested scope must be allowed for the client and exist as an enabled identity resource
    /// or API scope. offline_access is allowed when the client allows offline access.
    /// Returns null and the offending scopes when that fails.
    /// </summary>
    public async Task<(ResolvedScopes? Resolved, string? Invalid)> ResolveAsync(Client client, IEnumerable<string> requested)
    {
        var scopes = requested.Distinct(StringComparer.Ordinal).ToList();
        if (scopes.Count == 0)
            return (null, "");

        var allowed = client.AllowedScopes.Select(s => s.Scope).ToHashSet(StringComparer.Ordinal);
        var resourceScopes = scopes.Where(s => s != StandardScopes.OfflineAccess).ToList();

        var identity = await db.IdentityResources.AsNoTracking().Include(r => r.UserClaims)
            .Where(r => r.Enabled && resourceScopes.Contains(r.Name)).ToListAsync();
        var api = await db.ApiScopes.AsNoTracking().Include(s => s.UserClaims)
            .Where(s => s.Enabled && resourceScopes.Contains(s.Name)).ToListAsync();

        var known = identity.Select(r => r.Name).Concat(api.Select(s => s.Name)).ToHashSet(StringComparer.Ordinal);
        var invalid = scopes.Where(s => s == StandardScopes.OfflineAccess
                ? !client.AllowOfflineAccess
                : !allowed.Contains(s) || !known.Contains(s))
            .ToList();
        if (invalid.Count > 0)
            return (null, string.Join(" ", invalid));

        var apiNames = api.Select(s => s.Name).ToList();
        var apiResources = apiNames.Count == 0 ? [] : await db.ApiResources.AsNoTracking().Include(r => r.UserClaims)
            .Where(r => r.Enabled && r.Scopes.Any(s => apiNames.Contains(s.Scope))).ToListAsync();

        return (new ResolvedScopes(scopes, identity, api, apiResources), null);
    }
}
