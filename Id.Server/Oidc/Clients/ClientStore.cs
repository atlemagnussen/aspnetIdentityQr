using System.Net;
using System.Security.Cryptography;
using System.Text;
using Id.Lib.Data;
using Id.Lib.Entities;
using Id.Lib.Services;
using Microsoft.EntityFrameworkCore;

namespace Id.Server.Oidc.Clients;

/// <summary>Reads clients straight from the database (no cache), so admin changes apply immediately.</summary>
public class ClientStore(IdDbContext db)
{
    public Task<Client?> FindEnabledAsync(string? clientId)
    {
        if (string.IsNullOrEmpty(clientId) || clientId.Length > 200)
            return Task.FromResult<Client?>(null);

        return db.Clients.AsNoTracking()
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.ClientSecrets)
            .Include(c => c.IdentityProviderRestrictions)
            .Include(c => c.Claims)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.ClientId == clientId && c.Enabled && c.ProtocolType == ProtocolTypes.OpenIdConnect);
    }
}

/// <summary>
/// Client authentication at the token endpoint: client_secret_basic, client_secret_post, or none for
/// public clients (RequireClientSecret = false).
/// </summary>
public class ClientAuthenticator(ClientStore clients, TimeProvider time)
{
    public sealed record Result(Client? Client, string? Error, string? Description, bool UsedBasic);

    public async Task<Result> AuthenticateAsync(HttpRequest request, IFormCollection form)
    {
        string? clientId = null, secret = null;
        var basic = false;

        var header = request.Headers.Authorization.ToString();
        if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            basic = true;
            if (!TryParseBasic(header["Basic ".Length..].Trim(), out clientId, out secret))
                return new(null, OAuthErrors.InvalidClient, "Malformed Basic authorization header.", true);
            if (form.ContainsKey("client_secret"))
                return new(null, OAuthErrors.InvalidRequest, "Use one client authentication method.", true);
            if (form.TryGetValue("client_id", out var formId) && formId.ToString() != clientId)
                return new(null, OAuthErrors.InvalidRequest, "client_id doesn't match the authorization header.", true);
        }
        else
        {
            clientId = form["client_id"].ToString();
            secret = form.ContainsKey("client_secret") ? form["client_secret"].ToString() : null;
        }

        var client = await clients.FindEnabledAsync(clientId);
        if (client is null)
            return new(null, OAuthErrors.InvalidClient, null, basic);

        if (!client.RequireClientSecret)
            return new(client, null, null, basic);

        if (string.IsNullOrEmpty(secret) || !SecretMatches(client, secret))
            return new(null, OAuthErrors.InvalidClient, null, basic);

        return new(client, null, null, basic);
    }

    /// <summary>
    /// Stored shared secrets are base64(SHA-256) (what the admin app writes) or base64(SHA-512)
    /// (also accepted by Duende). Expired secrets don't count.
    /// </summary>
    private bool SecretMatches(Client client, string secret)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var sha256 = Encoding.UTF8.GetBytes(secret.Sha256());
        var sha512 = Encoding.UTF8.GetBytes(Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(secret))));

        var match = false;
        foreach (var s in client.ClientSecrets)
        {
            if (s.Type != SecretTypes.SharedSecret || (s.Expiration is { } exp && exp <= now))
                continue;
            var stored = Encoding.UTF8.GetBytes(s.Value);
            // No early exit: keep the timing independent of which secret matched.
            match |= CryptographicOperations.FixedTimeEquals(stored, sha256)
                     | CryptographicOperations.FixedTimeEquals(stored, sha512);
        }
        return match;
    }

    /// <summary>RFC 6749 2.3.1: id and secret are form-urlencoded before they are base64 encoded.</summary>
    private static bool TryParseBasic(string value, out string? clientId, out string? secret)
    {
        clientId = secret = null;
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            var colon = decoded.IndexOf(':');
            if (colon <= 0)
                return false;
            clientId = WebUtility.UrlDecode(decoded[..colon]);
            secret = WebUtility.UrlDecode(decoded[(colon + 1)..]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
