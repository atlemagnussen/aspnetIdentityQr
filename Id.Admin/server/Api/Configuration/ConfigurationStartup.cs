using Id.Admin.Api.Configuration.ApiResources;
using Id.Admin.Api.Configuration.Clients;
using Id.Admin.Api.Configuration.IdentityProviders;
using Id.Admin.Api.Configuration.IdentityResources;

namespace Id.Admin.Api.Configuration;

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