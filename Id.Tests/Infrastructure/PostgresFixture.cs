using Id.Lib.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Id.Tests.Infrastructure.PostgresFixture))]

namespace Id.Tests.Infrastructure;

/// <summary>
/// One throwaway Postgres for the whole test run (needs Docker or a Docker-compatible socket),
/// migrated with the real Id.Lib migrations. Tests share it, so use unique keys per test.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("docker.io/library/postgres:17").Build();

    public string ConnectionString => _container.GetConnectionString();

    public IdDbContext NewContext() => new(DatabaseSetup.StandaloneOptions(ConnectionString));

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = NewContext();
        await db.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
