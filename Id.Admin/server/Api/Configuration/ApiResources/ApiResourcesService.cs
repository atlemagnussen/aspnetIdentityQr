using Id.Lib.Data;
using Microsoft.EntityFrameworkCore;

namespace Id.Admin.Api.Configuration.ApiResources;

public class ApiResourceDto
{
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public required bool Enabled { get; set; }
    public required bool ShowInDiscoveryDocument { get; set; }
    public required bool RequireResourceIndicator { get; set; }
    public required List<string> AllowedAccessTokenSigningAlgorithms { get; set; }
    public required List<string> Scopes { get; set; }
    public required List<string> UserClaims { get; set; }
}

public class ApiResourcesService(IdDbContext context)
{
    public async Task<List<ApiResourceDto>> List() =>
        await Query().OrderBy(r => r.Name).ToListAsync();

    public async Task<ApiResourceDto> Get(string name) =>
        await Query().SingleOrDefaultAsync(r => r.Name == name)
        ?? throw new ApplicationException($"API resource '{name}' not found.");

    private IQueryable<ApiResourceDto> Query() =>
        context.ApiResources.AsNoTracking().Select(r => new ApiResourceDto
        {
            Name = r.Name,
            DisplayName = r.DisplayName,
            Description = r.Description,
            Enabled = r.Enabled,
            ShowInDiscoveryDocument = r.ShowInDiscoveryDocument,
            RequireResourceIndicator = r.RequireResourceIndicator,
            AllowedAccessTokenSigningAlgorithms = (r.AllowedAccessTokenSigningAlgorithms ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            Scopes = r.Scopes.Select(s => s.Scope).ToList(),
            UserClaims = r.UserClaims.Select(c => c.Type).ToList(),
        });
}
