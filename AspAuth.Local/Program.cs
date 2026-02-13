using AspAuth.Lib.Services;
using AspAuth.Local.Observe;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.ConfigureOtel()
    .AddOtelExporters();

builder.ConfigureCookies();
builder.ConfigureAspNetIdentity();
builder.AddWebAuthn();
builder.ConfigureIdentityServer();

builder.Services.LoadServices();

builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

//app.MigrateDb();

// app.SetPublicUrl(builder.Configuration);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseIdentityServer();

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
