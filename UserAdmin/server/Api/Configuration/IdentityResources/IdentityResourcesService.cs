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

    public async Task<IdentityResource> Get(string name)
    {
        var resource = await Context.IdentityResources
            .Include(i => i.UserClaims)
            .SingleOrDefaultAsync(i => i.Name == name)
            ?? throw new ApplicationException("not found");
        
        return resource.ToModel();
    }
}