using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace UserAdmin.Api.Configuration.Clients;

public class ClientsService(ConfigurationDbContext context)
{
    private readonly ConfigurationDbContext Context = context;

    public async Task<IEnumerable<Client>> List()
    {
        var clients = await Context.Clients
            .ToListAsync();
        var clientsDto = clients.Select( c => c.ToModel());
        return clientsDto;
    }

    public async Task<Client> Get(string clientId)
    {
        var client = await Context.Clients
            .Include(c => c.AllowedGrantTypes)
            .Include(c => c.RedirectUris)
            .Include(c => c.PostLogoutRedirectUris)
            .Include(c => c.AllowedScopes)
            .Include(c => c.IdentityProviderRestrictions)
            .Include(c => c.Claims)
            .Include(c => c.AllowedCorsOrigins)
            .Include(c => c.Properties)
            .SingleOrDefaultAsync(c => c.ClientId == clientId)
            ?? throw new ApplicationException($"Client {clientId} not found");
        
        return client.ToModel();
    }
}