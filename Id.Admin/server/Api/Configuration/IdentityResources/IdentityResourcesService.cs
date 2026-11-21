using Id.Lib.Data;
using Microsoft.EntityFrameworkCore;

namespace Id.Admin.Api.Configuration.IdentityResources;

public class IdentityResourceDto
{
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public required bool Enabled { get; set; }
    public required bool Required { get; set; }
    public required bool Emphasize { get; set; }
    public required bool ShowInDiscoveryDocument { get; set; }
    public required List<string> UserClaims { get; set; }
}

public class IdentityResourcesService(IdDbContext context)
{
    public async Task<List<IdentityResourceDto>> List() =>
        await Query().OrderBy(r => r.Name).ToListAsync();

    public async Task<IdentityResourceDto> Get(string name) =>
        await Query().SingleOrDefaultAsync(r => r.Name == name)
        ?? throw new ApplicationException($"Identity resource '{name}' not found.");

    private IQueryable<IdentityResourceDto> Query() =>
        context.IdentityResources.AsNoTracking().Select(r => new IdentityResourceDto
        {
            Name = r.Name,
            DisplayName = r.DisplayName,
            Description = r.Description,
            Enabled = r.Enabled,
            Required = r.Required,
            Emphasize = r.Emphasize,
            ShowInDiscoveryDocument = r.ShowInDiscoveryDocument,
            UserClaims = r.UserClaims.Select(c => c.Type).ToList(),
        });
}
