using AspAuth.Liv.Models;
using AspAuthLocal.Data;
using AspAuthLocal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

var configGoogle = configuration.GetSection("Authentication:Google");
var googleSettings = configGoogle.Get<AuthenticationClient>();
if (googleSettings is null)
    throw new ApplicationException("missing Google settings");

var configEmail = configuration.GetSection("Email");
services.Configure<EmailSettings>(configEmail);

// var DbPath = Path.Join(@"C:/temp", "aspnetauth.db");
// var connectionString = $"Data Source={DbPath}";
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:AuthDb");

services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var authBuilder = services.AddAuthentication();
//services.AddAuthorization(options =>
services.AddAuthentication()
    .AddGoogle(google => {
        google.ClientId = googleSettings.ClientId ?? throw new ApplicationException("missing google ClientId");
        google.ClientSecret = googleSettings.ClientSecret ?? throw new ApplicationException("missing google ClientSecret");
        google.CallbackPath = "/signin-google";
    });

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();


services.AddTransient<IEmailSender<IdentityUser>, AuthEmailSender>();
//authBuilder.AddOpenIdConnect("AzureAD", "Azure Active Directory", options =>
//{
//    options.SignInScheme = "idsrv.external";
//    options.SignOutScheme = "idsrv";

//    options.Authority = "https://login.microsoftonline.com/common";
//    options.ClientId = "973979fc-3d5c-45b8-b794-bea46316bac9";
//    options.ResponseType = "id_token";
//    options.CallbackPath = "/signin-aad";
//    options.SignedOutCallbackPath = "/signout-callback-aad";
//    options.RemoteSignOutPath = "/signout-aad";
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        NameClaimType = "name",
//        RoleClaimType = "role",
//        ValidateIssuer = false
//    };
//});

 

builder.Services.AddRazorPages();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
