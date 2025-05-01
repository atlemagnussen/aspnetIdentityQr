using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Local;

public class DataProtectionContext : DbContext, IDataProtectionKeyContext
{
    public DataProtectionContext(DbContextOptions<DataProtectionContext> options)
        : base(options) { }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
}