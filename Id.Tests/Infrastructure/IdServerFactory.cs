using System.Security.Cryptography;
using Id.Lib.Data;
using Id.Lib.Entities;
using Id.Lib.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

[assembly: AssemblyFixture(typeof(Id.Tests.Infrastructure.IdServerFactory))]

namespace Id.Tests.Infrastructure;

/// <summary>
/// The real Id.Server on its own test database, seeded with the clients, resources and user below.
/// Issuer is https://localhost (the test client's base address).
/// </summary>
public sealed class IdServerFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgresFixture pg = new();

    public const string Issuer = "https://localhost";
    public const string UserName = "alice@test.example";
    public const string Password = "Passw0rd!";
    public const string FullName = "Alice Test";

    /// <summary>Public SPA client: PKCE required, refresh tokens (one-time use).</summary>
    public const string Spa = "spa";
    public const string SpaRedirect = "https://app.test/cb";
    public const string SpaPostLogout = "https://app.test/";
    public const string SpaOrigin = "https://app.test";

    /// <summary>Confidential client without PKCE requirement.</summary>
    public const string Confidential = "conf";
    public const string ConfidentialSecret = "s3cret";
    public const string ConfidentialRedirect = "https://conf.test/cb";

    /// <summary>Relying party configured like Id.Admin (ASP.NET OIDC handler, public client).</summary>
    public const string Rp = "rp";
    public const string RpBase = "https://client.test";

    public const string ApiScope = "api1";
    public const string ApiResource = "apires";

    public string UserId { get; private set; } = "";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:AuthDb", pg.ConnectionString);
        builder.UseSetting("Authentication:Google:ClientId", "test");
        builder.UseSetting("Authentication:Google:ClientSecret", "test");
        builder.UseSetting("Authentication:EntraId:ClientId", "test");
        builder.UseSetting("Authentication:EntraId:ClientSecret", "test");
    }

    public Browser NewBrowser() => new(CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri(Issuer),
        AllowAutoRedirect = false,
        HandleCookies = true,
    }));

    public async ValueTask InitializeAsync()
    {
        await pg.InitializeAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdDbContext>();

        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP521);
        db.SigningKeys.Add(new CryptoSigningKey
        {
            StartDate = DateTime.UtcNow.AddDays(-1),
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            // No kid, like the production key.
            Key = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(new ECDsaSecurityKey(ec)),
        });

        db.IdentityResources.AddRange(
            new IdentityResource { Name = "openid", UserClaims = [new() { Type = "sub" }] },
            new IdentityResource { Name = "profile", UserClaims = [new() { Type = "name" }, new() { Type = "preferred_username" }] },
            new IdentityResource { Name = "roles", UserClaims = [new() { Type = "role" }] });
        db.ApiScopes.Add(new ApiScope { Name = ApiScope, UserClaims = [new() { Type = "role" }] });
        db.ApiResources.Add(new ApiResource { Name = ApiResource, Scopes = [new() { Scope = ApiScope }], UserClaims = [new() { Type = "email" }] });

        db.Clients.Add(new Client
        {
            ClientId = Spa,
            RequireClientSecret = false,
            RequirePkce = true,
            AllowOfflineAccess = true,
            RefreshTokenUsage = TokenUsage.OneTimeOnly,
            AllowedGrantTypes = [new() { GrantType = "authorization_code" }],
            RedirectUris = [new() { RedirectUri = SpaRedirect }],
            PostLogoutRedirectUris = [new() { PostLogoutRedirectUri = SpaPostLogout }],
            AllowedCorsOrigins = [new() { Origin = SpaOrigin }],
            AllowedScopes = [new() { Scope = "openid" }, new() { Scope = "profile" }, new() { Scope = "roles" }, new() { Scope = ApiScope }],
        });
        db.Clients.Add(new Client
        {
            ClientId = Confidential,
            RequirePkce = false,
            ClientSecrets = [new() { Value = ConfidentialSecret.Sha256() }],
            AllowedGrantTypes = [new() { GrantType = "authorization_code" }],
            RedirectUris = [new() { RedirectUri = ConfidentialRedirect }],
            AllowedScopes = [new() { Scope = "openid" }, new() { Scope = "profile" }],
        });
        db.Clients.Add(new Client
        {
            ClientId = Rp,
            RequireClientSecret = false,
            AllowedGrantTypes = [new() { GrantType = "authorization_code" }],
            RedirectUris = [new() { RedirectUri = RpBase + "/signin-oidc" }],
            AllowedScopes = [new() { Scope = "openid" }, new() { Scope = "profile" }, new() { Scope = "roles" }],
        });
        await db.SaveChangesAsync();

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roles.CreateAsync(new IdentityRole("Admin"));
        var user = new ApplicationUser { UserName = UserName, Email = UserName, EmailConfirmed = true };
        Check(await users.CreateAsync(user, Password));
        Check(await users.AddToRoleAsync(user, "Admin"));
        db.UserProfiles.Add(new UserProfile { UserId = user.Id, FullName = FullName });
        await db.SaveChangesAsync();
        UserId = user.Id;
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await base.DisposeAsync();
        await pg.DisposeAsync();
    }
}
