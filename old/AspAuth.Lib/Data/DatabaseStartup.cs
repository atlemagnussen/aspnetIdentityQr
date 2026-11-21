using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AspAuth.Lib.Data;

public static class ApplicationDatabaseStartup
{
    public static void AddApplicationDatabaseContext(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseNpgsql(connectionString, o =>
            {
                o.ConfigureDataSource(ds => ds.EnableDynamicJson());
            });
        });
    }
}