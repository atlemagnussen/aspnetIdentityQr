using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Id.Admin.Api.Configuration.Clients.Models;
using Id.Admin.Auth;
using Id.Lib.Data;
using Id.Lib.Entities;
using Id.Lib.Oidc;
using Id.Lib.Services;

namespace Id.Admin.Api.Configuration.Clients;

/// <summary>
/// Full CRUD on OAuth/OIDC clients. The ID server reads clients from the database on every request (no cache),
/// so changes apply to the next request there.
/// </summary>
public partial class ClientsService(IdDbContext context, AdminOptions adminOptions)
{
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._:-]{0,199}$")]
    private static partial Regex ValidClientId();

    public async Task<List<ClientSummaryDto>> List()
    {
        return await context.Clients.AsNoTracking()
            .OrderBy(c => c.ClientId)
            .Select(c => new ClientSummaryDto
            {
                ClientId = c.ClientId,
                ClientName = c.ClientName,
                Enabled = c.Enabled,
                AllowedGrantTypes = c.AllowedGrantTypes.Select(g => g.GrantType).ToList(),
                RequireClientSecret = c.RequireClientSecret,
                SecretCount = c.ClientSecrets.Count,
                RedirectUriCount = c.RedirectUris.Count,
                AllowedScopeCount = c.AllowedScopes.Count,
                LastAccessed = c.LastAccessed,
                IsProtected = c.ClientId == adminOptions.AuthClient,
            })
            .ToListAsync();
    }

    public async Task<ClientDto> Get(string clientId) => ToDto(await FullClient(clientId));

    public async Task<ClientDto> Create(ClientDto dto)
    {
        var clientId = dto.ClientId?.Trim() ?? "";
        if (!ValidClientId().IsMatch(clientId))
            throw new ApplicationException("Client id is 1-200 characters: letters, digits and . _ : - (no spaces).");
        if (await context.Clients.AnyAsync(c => c.ClientId == clientId))
            throw new ApplicationException($"A client with id '{clientId}' already exists.");

        var entity = new Client
        {
            ClientId = clientId,
            Created = DateTime.UtcNow,
            ClientSecrets = [],
            AllowedGrantTypes = [],
            RedirectUris = [],
            PostLogoutRedirectUris = [],
            AllowedScopes = [],
            IdentityProviderRestrictions = [],
            Claims = [],
            AllowedCorsOrigins = [],
            Properties = [],
        };
        await Apply(entity, dto, isNew: true);

        context.Clients.Add(entity);
        await context.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task<ClientDto> Update(string clientId, ClientDto dto)
    {
        var entity = await FullClient(clientId);
        if (entity.NonEditable)
            throw new ApplicationException($"Client '{clientId}' is marked non-editable.");
        if (!string.Equals(dto.ClientId?.Trim(), clientId, StringComparison.Ordinal))
            throw new ApplicationException("The client id can't be changed. Create a new client instead.");
        if (IsProtected(clientId) && !dto.Enabled)
            throw new ApplicationException($"'{clientId}' is the client this admin app signs in with; disabling it would lock you out.");

        await Apply(entity, dto, isNew: false);
        entity.Updated = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return ToDto(entity);
    }

    public async Task Delete(string clientId)
    {
        if (IsProtected(clientId))
            throw new ApplicationException($"'{clientId}' is the client this admin app signs in with and can't be deleted.");
        var entity = await FullClient(clientId);
        context.Clients.Remove(entity); // child tables cascade
        await context.SaveChangesAsync();
    }

    // ---- secrets ----

    public async Task<List<ClientSecretDto>> ListSecrets(string clientId)
    {
        var client = await context.Clients.AsNoTracking()
            .Include(c => c.ClientSecrets)
            .SingleOrDefaultAsync(c => c.ClientId == clientId)
            ?? throw NotFound(clientId);
        return [.. client.ClientSecrets.OrderByDescending(s => s.Created).Select(ToDto)];
    }

    public async Task<ClientSecretCreatedDto> CreateSecret(string clientId, ClientSecretCreateDto dto)
    {
        if (dto.Expiration is { } exp && exp <= DateTime.UtcNow)
            throw new ApplicationException("Expiration must be in the future.");

        var client = await context.Clients
            .Include(c => c.ClientSecrets)
            .SingleOrDefaultAsync(c => c.ClientId == clientId)
            ?? throw NotFound(clientId);

        var value = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var secret = new ClientSecret
        {
            Value = value.Sha256(), // base64(SHA-256): the format the ID server validates against
            Type = SecretTypes.SharedSecret,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Expiration = dto.Expiration is { } e ? DateTime.SpecifyKind(e, DateTimeKind.Utc) : null,
            Created = DateTime.UtcNow,
        };
        client.ClientSecrets.Add(secret);
        client.Updated = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new ClientSecretCreatedDto { Secret = ToDto(secret), Value = value };
    }

    public async Task DeleteSecret(string clientId, int secretId)
    {
        var client = await context.Clients
            .Include(c => c.ClientSecrets)
            .SingleOrDefaultAsync(c => c.ClientId == clientId)
            ?? throw NotFound(clientId);
        var secret = client.ClientSecrets.SingleOrDefault(s => s.Id == secretId)
            ?? throw new ApplicationException($"Secret {secretId} not found.");
        client.ClientSecrets.Remove(secret);
        client.Updated = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    // ---- scopes lookup ----

    public async Task<List<ScopeDto>> ListScopes()
    {
        var identity = await context.IdentityResources.AsNoTracking()
            .Select(r => new ScopeDto { Name = r.Name, DisplayName = r.DisplayName, Kind = "identity", Enabled = r.Enabled })
            .ToListAsync();
        var api = await context.ApiScopes.AsNoTracking()
            .Select(s => new ScopeDto { Name = s.Name, DisplayName = s.DisplayName, Kind = "api", Enabled = s.Enabled })
            .ToListAsync();
        return [.. identity.OrderBy(s => s.Name), .. api.OrderBy(s => s.Name)];
    }

    // ---- mapping / validation ----

    private async Task Apply(Client e, ClientDto d, bool isNew)
    {
        var grantTypes = Clean(d.AllowedGrantTypes);
        if (grantTypes.Count == 0)
            throw new ApplicationException("Select at least one grant type.");
        try { GrantTypes.Validate(grantTypes); }
        catch (InvalidOperationException ex) { throw new ApplicationException(ex.Message); }

        // Scopes: newly added ones must exist (existing unknown ones are kept so old data doesn't block saving)
        var scopes = Clean(d.AllowedScopes);
        if (scopes.Contains(StandardScopes.OfflineAccess))
            throw new ApplicationException("Don't add 'offline_access' as a scope; use 'Allow offline access' (refresh tokens).");
        var existingScopes = isNew ? [] : e.AllowedScopes.Select(s => s.Scope).ToHashSet();
        var known = (await ListScopes()).Select(s => s.Name).ToHashSet();
        var unknown = scopes.Where(s => !known.Contains(s) && !existingScopes.Contains(s)).ToList();
        if (unknown.Count > 0)
            throw new ApplicationException($"Unknown scope(s): {string.Join(", ", unknown)}.");

        var lifetimes = new (string Name, int Value)[]
        {
            ("Access token lifetime", d.AccessTokenLifetime),
            ("Identity token lifetime", d.IdentityTokenLifetime),
            ("Authorization code lifetime", d.AuthorizationCodeLifetime),
        };
        foreach (var (name, value) in lifetimes)
            if (value <= 0) throw new ApplicationException($"{name} must be greater than 0 seconds.");
        if (d.AbsoluteRefreshTokenLifetime < 0 || d.SlidingRefreshTokenLifetime < 0)
            throw new ApplicationException("Refresh token lifetimes can't be negative (0 = no limit for absolute).");

        e.ClientName = Text(d.ClientName);
        e.Description = Text(d.Description);
        e.ClientUri = OptionalUrl(d.ClientUri, "Client URI");
        e.LogoUri = OptionalUrl(d.LogoUri, "Logo URI");
        e.Enabled = d.Enabled;
        e.ProtocolType = string.IsNullOrWhiteSpace(d.ProtocolType) ? ProtocolTypes.OpenIdConnect : d.ProtocolType.Trim();

        e.RequireClientSecret = d.RequireClientSecret;
        e.RequirePkce = d.RequirePkce;
        e.AllowPlainTextPkce = d.AllowPlainTextPkce;
        e.RequireRequestObject = d.RequireRequestObject;
        e.RequireDPoP = d.RequireDPoP;
        e.RequirePushedAuthorization = d.RequirePushedAuthorization;
        e.AllowAccessTokensViaBrowser = d.AllowAccessTokensViaBrowser;
        e.AllowOfflineAccess = d.AllowOfflineAccess;
        e.EnableLocalLogin = d.EnableLocalLogin;

        e.RequireConsent = d.RequireConsent;
        e.AllowRememberConsent = d.AllowRememberConsent;
        e.ConsentLifetime = d.ConsentLifetime is > 0 ? d.ConsentLifetime : null;

        e.FrontChannelLogoutUri = OptionalUrl(d.FrontChannelLogoutUri, "Front-channel logout URI");
        e.FrontChannelLogoutSessionRequired = d.FrontChannelLogoutSessionRequired;
        e.BackChannelLogoutUri = OptionalUrl(d.BackChannelLogoutUri, "Back-channel logout URI");
        e.BackChannelLogoutSessionRequired = d.BackChannelLogoutSessionRequired;
        e.InitiateLoginUri = OptionalUrl(d.InitiateLoginUri, "Initiate login URI");

        e.AccessTokenType = d.AccessTokenType;
        e.AccessTokenLifetime = d.AccessTokenLifetime;
        e.IdentityTokenLifetime = d.IdentityTokenLifetime;
        e.AuthorizationCodeLifetime = d.AuthorizationCodeLifetime;
        e.AbsoluteRefreshTokenLifetime = d.AbsoluteRefreshTokenLifetime;
        e.SlidingRefreshTokenLifetime = d.SlidingRefreshTokenLifetime;
        e.RefreshTokenUsage = d.RefreshTokenUsage;
        e.RefreshTokenExpiration = d.RefreshTokenExpiration;
        e.UpdateAccessTokenClaimsOnRefresh = d.UpdateAccessTokenClaimsOnRefresh;
        e.IncludeJwtId = d.IncludeJwtId;
        e.AlwaysIncludeUserClaimsInIdToken = d.AlwaysIncludeUserClaimsInIdToken;
        e.UserSsoLifetime = d.UserSsoLifetime is > 0 ? d.UserSsoLifetime : null;
        e.CoordinateLifetimeWithUserSession = d.CoordinateLifetimeWithUserSession;
        var algs = Clean(d.AllowedIdentityTokenSigningAlgorithms);
        e.AllowedIdentityTokenSigningAlgorithms = algs.Count == 0 ? null : string.Join(",", algs);

        e.AlwaysSendClientClaims = d.AlwaysSendClientClaims;
        e.ClientClaimsPrefix = d.ClientClaimsPrefix?.Trim() ?? "";
        e.PairWiseSubjectSalt = Text(d.PairWiseSubjectSalt);

        Sync(e.AllowedGrantTypes, grantTypes, x => x.GrantType, v => new ClientGrantType { GrantType = v });
        Sync(e.AllowedScopes, scopes, x => x.Scope, v => new ClientScope { Scope = v });
        Sync(e.RedirectUris, Clean(d.RedirectUris.Select(u => AbsoluteUrl(u, "Redirect URI"))),
            x => x.RedirectUri, v => new ClientRedirectUri { RedirectUri = v });
        Sync(e.PostLogoutRedirectUris, Clean(d.PostLogoutRedirectUris.Select(u => AbsoluteUrl(u, "Post-logout redirect URI"))),
            x => x.PostLogoutRedirectUri, v => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = v });
        Sync(e.AllowedCorsOrigins, Clean(d.AllowedCorsOrigins.Select(CorsOrigin)),
            x => x.Origin, v => new ClientCorsOrigin { Origin = v });
        Sync(e.IdentityProviderRestrictions, Clean(d.IdentityProviderRestrictions),
            x => x.Provider, v => new ClientIdPRestriction { Provider = v });

        var claims = d.Claims
            .Select(c => (Type: c.Type?.Trim() ?? "", Value: c.Value?.Trim() ?? ""))
            .Where(c => c.Type != "" && c.Value != "")
            .Distinct().ToList();
        Sync(e.Claims, claims.Select(c => $"{c.Type}\n{c.Value}").ToList(), x => $"{x.Type}\n{x.Value}",
            v => { var parts = v.Split('\n', 2); return new ClientClaim { Type = parts[0], Value = parts[1] }; });

        var props = d.Properties
            .Select(p => (Key: p.Key?.Trim() ?? "", Value: p.Value ?? ""))
            .Where(p => p.Key != "").ToList();
        var dupKey = props.GroupBy(p => p.Key).FirstOrDefault(g => g.Count() > 1);
        if (dupKey is not null)
            throw new ApplicationException($"Duplicate property key '{dupKey.Key}'.");
        foreach (var existing in e.Properties.ToList())
        {
            var match = props.FirstOrDefault(p => p.Key == existing.Key);
            if (match.Key is null) e.Properties.Remove(existing);
            else existing.Value = match.Value;
        }
        foreach (var p in props.Where(p => e.Properties.All(x => x.Key != p.Key)))
            e.Properties.Add(new ClientProperty { Key = p.Key, Value = p.Value });
    }

    /// <summary>Make a child collection match the desired values: remove missing (EF deletes orphans), add new.</summary>
    private static void Sync<T>(List<T> current, List<string> desired, Func<T, string> key, Func<string, T> create)
    {
        current.RemoveAll(x => !desired.Contains(key(x)));
        foreach (var value in desired.Where(v => current.All(x => key(x) != v)))
            current.Add(create(value));
    }

    private ClientDto ToDto(Client c) => new()
    {
        ClientId = c.ClientId,
        ClientName = c.ClientName,
        Description = c.Description,
        ClientUri = c.ClientUri,
        LogoUri = c.LogoUri,
        Enabled = c.Enabled,
        ProtocolType = c.ProtocolType,
        AllowedGrantTypes = [.. c.AllowedGrantTypes.Select(x => x.GrantType)],
        RequireClientSecret = c.RequireClientSecret,
        RequirePkce = c.RequirePkce,
        AllowPlainTextPkce = c.AllowPlainTextPkce,
        RequireRequestObject = c.RequireRequestObject,
        RequireDPoP = c.RequireDPoP,
        RequirePushedAuthorization = c.RequirePushedAuthorization,
        AllowAccessTokensViaBrowser = c.AllowAccessTokensViaBrowser,
        AllowOfflineAccess = c.AllowOfflineAccess,
        EnableLocalLogin = c.EnableLocalLogin,
        IdentityProviderRestrictions = [.. c.IdentityProviderRestrictions.Select(x => x.Provider)],
        RequireConsent = c.RequireConsent,
        AllowRememberConsent = c.AllowRememberConsent,
        ConsentLifetime = c.ConsentLifetime,
        RedirectUris = [.. c.RedirectUris.Select(x => x.RedirectUri).Order()],
        PostLogoutRedirectUris = [.. c.PostLogoutRedirectUris.Select(x => x.PostLogoutRedirectUri).Order()],
        AllowedCorsOrigins = [.. c.AllowedCorsOrigins.Select(x => x.Origin).Order()],
        FrontChannelLogoutUri = c.FrontChannelLogoutUri,
        FrontChannelLogoutSessionRequired = c.FrontChannelLogoutSessionRequired,
        BackChannelLogoutUri = c.BackChannelLogoutUri,
        BackChannelLogoutSessionRequired = c.BackChannelLogoutSessionRequired,
        InitiateLoginUri = c.InitiateLoginUri,
        AllowedScopes = [.. c.AllowedScopes.Select(x => x.Scope)],
        AccessTokenType = c.AccessTokenType,
        AccessTokenLifetime = c.AccessTokenLifetime,
        IdentityTokenLifetime = c.IdentityTokenLifetime,
        AuthorizationCodeLifetime = c.AuthorizationCodeLifetime,
        AbsoluteRefreshTokenLifetime = c.AbsoluteRefreshTokenLifetime,
        SlidingRefreshTokenLifetime = c.SlidingRefreshTokenLifetime,
        RefreshTokenUsage = c.RefreshTokenUsage,
        RefreshTokenExpiration = c.RefreshTokenExpiration,
        UpdateAccessTokenClaimsOnRefresh = c.UpdateAccessTokenClaimsOnRefresh,
        IncludeJwtId = c.IncludeJwtId,
        AlwaysIncludeUserClaimsInIdToken = c.AlwaysIncludeUserClaimsInIdToken,
        UserSsoLifetime = c.UserSsoLifetime,
        CoordinateLifetimeWithUserSession = c.CoordinateLifetimeWithUserSession ?? false,
        AllowedIdentityTokenSigningAlgorithms = [.. (c.AllowedIdentityTokenSigningAlgorithms ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
        Claims = [.. c.Claims.Select(x => new ClientClaimDto { Type = x.Type, Value = x.Value })],
        AlwaysSendClientClaims = c.AlwaysSendClientClaims,
        ClientClaimsPrefix = c.ClientClaimsPrefix,
        PairWiseSubjectSalt = c.PairWiseSubjectSalt,
        Properties = [.. c.Properties.Select(x => new ClientPropertyDto { Key = x.Key, Value = x.Value })],
        Created = c.Created,
        Updated = c.Updated,
        LastAccessed = c.LastAccessed,
        NonEditable = c.NonEditable,
        IsProtected = IsProtected(c.ClientId),
    };

    private static ClientSecretDto ToDto(ClientSecret s) => new()
    {
        Id = s.Id,
        Description = s.Description,
        Type = s.Type,
        Expiration = s.Expiration,
        Created = s.Created,
    };

    private async Task<Client> FullClient(string clientId) =>
        await context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.IdentityProviderRestrictions)
            .Include(c => c.Claims)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.Properties)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.ClientId == clientId)
        ?? throw NotFound(clientId);

    private bool IsProtected(string clientId) => clientId == adminOptions.AuthClient;

    private static ApplicationException NotFound(string clientId) => new($"Client '{clientId}' not found.");

    private static List<string> Clean(IEnumerable<string?> values) =>
        [.. values.Select(v => v?.Trim()).Where(v => !string.IsNullOrEmpty(v)).Select(v => v!).Distinct()];

    private static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? OptionalUrl(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : AbsoluteUrl(value, name);

    /// <summary>Any absolute URI; custom schemes are allowed (native apps).</summary>
    private static string AbsoluteUrl(string? value, string name)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            throw new ApplicationException($"{name} '{value}' must be an absolute URL.");
        return trimmed;
    }

    /// <summary>CORS origins are scheme + host + optional port only, no path or trailing slash.</summary>
    private static string CorsOrigin(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ApplicationException($"CORS origin '{value}' must be an absolute http(s) URL.");
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
