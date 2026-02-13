using AspAuth.Lib.Data;
using AspAuth.Lib.Models;

namespace UserAdmin.Persistence.Database;

public static class DatabaseStartup
{
    public static void AddDatabase(this WebApplicationBuilder builder, string connectionString)
    {
        builder.AddApplicationDatabaseContext(connectionString);
    }
}