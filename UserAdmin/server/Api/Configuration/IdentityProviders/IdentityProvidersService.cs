using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace UserAdmin.Api.Configuration.IdentityProviders;

public class IdentityProvidersService(ConfigurationDbContext context)
{
    private readonly ConfigurationDbContext Context = context;

    public async Task<IEnumerable<IdentityProvider>> List()
    {
        var providers = await Context.IdentityProviders.ToArrayAsync();
        return providers.Select(p => p.ToModel());
    }
}