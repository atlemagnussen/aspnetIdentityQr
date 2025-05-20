using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Data;

public class ApplicationConfigurationDbContext : ConfigurationDbContext
{
    public ApplicationConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
            : base(options)
        {
        }
}