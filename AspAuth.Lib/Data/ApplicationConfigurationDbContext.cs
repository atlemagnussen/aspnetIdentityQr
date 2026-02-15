using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Data;

/// <summary>
/// Just to be able to do migrations, should not really use it
/// </summary>
public class ApplicationConfigurationDbContext : ConfigurationDbContext
{
    public ApplicationConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
            : base(options)
        {
        }
}