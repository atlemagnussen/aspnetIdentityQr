using Id.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Id.Tests.Lib;

public class SchemaTests(PostgresFixture pg)
{
    [Fact]
    public async Task Model_matches_migrations()
    {
        // If this fails, an entity/mapping changed without a migration. The schema is shared with
        // existing databases, so any change needs a deliberate migration in Id.Lib.
        await using var db = pg.NewContext();
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task All_migrations_are_applied()
    {
        await using var db = pg.NewContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Identity_schema_v3_includes_passkeys()
    {
        await using var db = pg.NewContext();
        Assert.Equal(0, await db.UserPasskeys.CountAsync(TestContext.Current.CancellationToken));
    }
}
