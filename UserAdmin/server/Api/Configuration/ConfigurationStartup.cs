using UserAdmin.Api.Configuration.Clients;

namespace UserAdmin.Api.Configuration;

public static class ConfigurationStartup
{
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ClientsService>();
    }
}