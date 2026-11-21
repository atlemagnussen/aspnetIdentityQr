using Id.Lib.Observe;
using Id.Lib.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Id.Admin.Api;
using Id.Admin.Auth;
using Id.Admin.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.ConfigureOtel()
    .AddOtelExporters();
    
builder.Services.AddProblemDetails(configure =>
{
    configure.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var adminOptions = new AdminOptions(
    AuthServer: Environment.GetEnvironmentVariable("AUTH_SERVER") ?? "https://id.logout.work",
    AuthClient: Environment.GetEnvironmentVariable("AUTH_CLIENT") ?? "web");
builder.Services.AddSingleton(adminOptions);

builder.AddAuthentication(adminOptions);
builder.AddPersistence();
builder.AddApi();
builder.Services.AddOpsHealthChecks();

builder.Services.AddRazorPages();

var app = builder.Build();

// must be first
app.UseForwardedHeaders();

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
    app.UseHsts();
}
app.UseExceptionHandler();

app.MapControllers();
app.MapRazorPages();
app.MapOpsEndpoints();

app.Run();
