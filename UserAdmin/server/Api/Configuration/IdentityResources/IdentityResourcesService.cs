using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace UserAdmin.Api.Configuration.IdentityResources;

public class IdentityResourcesService(ConfigurationDbContext context)
{
    private readonly ConfigurationDbContext Context = context;

    public async Task<IEnumerable<IdentityResource>> List()
    {
        var resources = await Context.IdentityResources.ToArrayAsync();
        return resources.Select(p => p.ToModel());
    }
}