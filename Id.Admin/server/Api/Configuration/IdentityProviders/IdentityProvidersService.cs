using Id.Lib.Data;
using Microsoft.EntityFrameworkCore;

namespace Id.Admin.Api.Configuration.IdentityProviders;

public class IdentityProviderDto
{
    public required string Scheme { get; set; }
    public string? DisplayName { get; set; }
    public required bool Enabled { get; set; }
    public required string Type { get; set; }
}

public class IdentityProvidersService(IdDbContext context)
{
    public async Task<List<IdentityProviderDto>> List() =>
        await context.IdentityProviders.AsNoTracking()
            .OrderBy(p => p.Scheme)
            .Select(p => new IdentityProviderDto { Scheme = p.Scheme, DisplayName = p.DisplayName, Enabled = p.Enabled, Type = p.Type })
            .ToListAsync();
}
