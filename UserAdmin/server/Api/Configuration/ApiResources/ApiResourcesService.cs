using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace UserAdmin.Api.Configuration.ApiResources;

public class ApiResourcesService(ConfigurationDbContext context)
{
    private readonly ConfigurationDbContext Context = context;

    public async Task<IEnumerable<ApiResource>> List()
    {
        var resources = await Context.ApiResources.ToListAsync();
        var resourcesDto = resources.Select( c => c.ToModel());
        return resourcesDto;
    }

    public async Task<ApiResource> Get(string name)
    {
        var resource = await Context.ApiResources
            .Include(a => a.Scopes)
            .Include(a => a.UserClaims)
            .SingleOrDefaultAsync(a => a.Name == name)
            ?? throw new ApplicationException("Not found");
        return resource.ToModel();
    }
}