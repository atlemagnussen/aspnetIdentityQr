using UserAdmin.Persistence.Database;

namespace UserAdmin.Persistence;

public static class PersistenceStartup
{
    public static void AddPersistence(this WebApplicationBuilder builder)
    {
        builder.AddDatabase();
    }
}