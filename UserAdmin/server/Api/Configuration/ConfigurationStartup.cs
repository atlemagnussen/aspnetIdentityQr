using UserAdmin.Api.Configuration.ApiResources;
using UserAdmin.Api.Configuration.Clients;
using UserAdmin.Api.Configuration.IdentityProviders;
using UserAdmin.Api.Configuration.IdentityResources;

namespace UserAdmin.Api.Configuration;

public static class ConfigurationStartup
{
    public static void AddConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ClientsService>();
        builder.Services.AddScoped<ApiResourcesService>();
        builder.Services.AddScoped<IdentityProvidersService>();
        builder.Services.AddScoped<IdentityResourcesService>();
    }
}