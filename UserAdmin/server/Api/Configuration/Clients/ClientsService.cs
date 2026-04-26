using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using DuendeDb = Duende.IdentityServer.EntityFramework.Entities;

namespace UserAdmin.Api.Configuration.Clients;

public class ClientsService(ConfigurationDbContext context)
{
    private readonly ConfigurationDbContext Context = context;

    public IQueryable<DuendeDb.Client> Clients =>
		Context.Clients;

    public IQueryable<DuendeDb.Client> Client(string clientId) =>
        Clients.Where(c => c.ClientId == clientId);

    public async Task<IEnumerable<Client>> List()
    {
        var clients = await Context.Clients
            .ToListAsync();
        var clientsDto = clients.Select( c => c.ToModel());
        return clientsDto;
    }

    private async Task<DuendeDb.Client> FullClient(string clientId)
    {
        var client = await Client(clientId)
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.IdentityProviderRestrictions)
            .Include(c => c.Claims)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.Properties)
            .FirstOrDefaultAsync()
            ?? throw new ApplicationException($"Client {clientId} not found");
        return client;
    }
    public async Task<Client> Get(string clientId)
    {
        var client = await FullClient(clientId);
        return client.ToModel();
    }

    public async Task<Client> AddRedirectUri(string clientId, string url)
    {
        var client = await FullClient(clientId);
        var normalizedUrl = NormalizeRedirectUri(url);

        var alreadyExists = client.RedirectUris.Any(r =>
            string.Equals(NormalizeRedirectUri(r.RedirectUri), normalizedUrl, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            return client.ToModel();

        client.RedirectUris.Add(new DuendeDb.ClientRedirectUri
        {
            RedirectUri = normalizedUrl,
            ClientId = client.Id
        });

        await Context.SaveChangesAsync();
        return client.ToModel();
    }

    public async Task<Client> AddPostLogoutRedirectUri(string clientId, string url)
    {
        var client = await FullClient(clientId);
        var normalizedUrl = NormalizeRedirectUri(url);

        var alreadyExists = client.PostLogoutRedirectUris.Any(r =>
            string.Equals(NormalizeRedirectUri(r.PostLogoutRedirectUri), normalizedUrl, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            return client.ToModel();

        client.PostLogoutRedirectUris.Add(new DuendeDb.ClientPostLogoutRedirectUri
        {
            PostLogoutRedirectUri = normalizedUrl,
            ClientId = client.Id
        });

        await Context.SaveChangesAsync();
        return client.ToModel();
    }

    public async Task<Client> AddCorsOrigin(string clientId, string url)
    {
        var client = await FullClient(clientId);
        var normalizedOrigin = NormalizeCorsOrigin(url);

        var alreadyExists = client.AllowedCorsOrigins.Any(o =>
            string.Equals(NormalizeCorsOrigin(o.Origin), normalizedOrigin, StringComparison.OrdinalIgnoreCase));

        if (alreadyExists)
            return client.ToModel();

        client.AllowedCorsOrigins.Add(new DuendeDb.ClientCorsOrigin {
            ClientId = client.Id,
            Origin = normalizedOrigin
        });

        await Context.SaveChangesAsync();
        return client.ToModel();
    }

    private static string NormalizeRedirectUri(string url)
    {
        var trimmed = url?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Redirect URL must be a valid absolute URL.", nameof(url));
        }

        return uri.AbsoluteUri;
    }

    private static string NormalizeCorsOrigin(string url)
    {
        var trimmed = url?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("CORS origin must be a valid absolute URL.", nameof(url));
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("CORS origin must use http or https.", nameof(url));
        }

        // Persist only the origin portion: scheme + host + optional non-default port.
        return uri.GetLeftPart(UriPartial.Authority);
    }
}