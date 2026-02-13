using AspAuth.Lib.Services;
using UserAdmin.Persistence.Database;

namespace UserAdmin.Persistence;

public static class PersistenceStartup
{
    public static void AddPersistence(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetAuthConnectionString();
        builder.AddDatabase(connectionString);
    }
}