using Microsoft.EntityFrameworkCore.Design;

namespace Id.Lib.Data;

/// <summary>
/// Lets `dotnet ef` run against Id.Lib without a startup project:
///   dotnet ef migrations add Name --project Id.Lib
///   ConnectionStrings__AuthDb="..." dotnet ef database update --project Id.Lib
/// </summary>
public class IdDbContextFactory : IDesignTimeDbContextFactory<IdDbContext>
{
    public IdDbContext CreateDbContext(string[] args)
    {
        // Adding a migration doesn't connect, so a placeholder is fine there.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AuthDb")
            ?? "Host=localhost;Database=auth;Username=postgres";
        return new IdDbContext(DatabaseSetup.StandaloneOptions(connectionString));
    }
}
