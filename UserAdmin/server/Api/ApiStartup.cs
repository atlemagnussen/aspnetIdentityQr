using System.Text.Json;
using System.Text.Json.Serialization;
using UserAdmin.Api.Configuration;
using UserAdmin.Api.Users;

namespace UserAdmin.Api;

public static class ApiStartup
{
    private static void ConfigureJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.NumberHandling = JsonNumberHandling.Strict;
        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    }

    public static void AddApi(this WebApplicationBuilder builder)
    {
        // Controllers use MVC JsonOptions, the OpenAPI generator reads the minimal-API (Http) JsonOptions.
        // Configure both identically so the generated client types match what the API actually sends.
        builder.Services.AddControllers()
            .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
        builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

        builder.AddUsers();
        builder.AddConfiguration();

        builder.Services.AddOpenApi(options =>
            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1
        );
    }
}