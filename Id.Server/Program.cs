using Id.Lib.Observe;
using Id.Server.Oidc;
using Id.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Behind the reverse proxy: trust all proxies.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.ConfigureOtel()
    .AddOtelExporters();

builder.AddIdentityServerIdentity();
builder.AddOidcServer();
builder.Services.AddScoped<WebAuthnService>();
builder.Services.AddOpsHealthChecks();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapOidcEndpoints();
app.MapControllers();
app.MapRazorPages();
app.MapOpsEndpoints();

app.Run();

public partial class Program;
