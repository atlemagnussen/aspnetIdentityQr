using AspAuth.Lib.Services;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAspNetIdentity();
builder.ConfigureIdentityServer();

builder.Services.LoadServices();

builder.Services.AddRazorPages();

var app = builder.Build();

//app.InitializeDatabase();

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

app.MapRazorPages();

app.Run();
