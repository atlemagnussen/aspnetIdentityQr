using System.Text.Json;
using System.Text.Json.Serialization;
using UserAdmin.Api.Users;

namespace UserAdmin.Api;

public static class ApiStartup
{
    public static void AddApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        });

        builder.AddUsers();

        builder.Services.AddOpenApi(options =>
            options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_1
        );
    }
}