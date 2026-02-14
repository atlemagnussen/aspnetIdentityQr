using AspAuth.Lib.Services;
using Microsoft.AspNetCore.HttpOverrides;
using UserAdmin.Api;
using UserAdmin.Auth;
using UserAdmin.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddProblemDetails(configure =>
{
    configure.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.ConfigureCookies();

var authServer = Environment.GetEnvironmentVariable("AUTH_SERVER") ?? "https://id.logout.work";
var authClient = Environment.GetEnvironmentVariable("AUTH_CLIENT") ?? "web";

builder.AddAuthentication(authServer, authClient);
builder.AddPersistence();
builder.AddApi();

builder.Services.AddRazorPages();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseForwardedHeaders();
}
app.UseExceptionHandler();

app.MapControllers();
app.MapRazorPages();

app.Run();
