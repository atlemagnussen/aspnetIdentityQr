using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Services;

public class SeedIdentityServerSetup(ConfigurationDbContext context)
{
    private readonly ConfigurationDbContext _context = context;

    public async Task SeedData()
    {
        // IdentityResources
        if (!await _context.IdentityResources.AnyAsync())
        {
            foreach(var resource in IdentityResources.ToList())
                await _context.IdentityResources.AddAsync(resource.ToEntity());
            await _context.SaveChangesAsync();
        }

        // ApiScopes
        if (!await _context.ApiScopes.AnyAsync())
        {
            foreach (var scope in ApiScopes.ToList())
            {
                var ent = scope.ToEntity();
                await _context.ApiScopes.AddAsync(ent);
            }
            await _context.SaveChangesAsync();
        }

        // ApiResources
        if (!await _context.ApiResources.AnyAsync())
        {
            foreach (var resource in ApiResources.ToList())
                await _context.ApiResources.AddAsync(resource.ToEntity());
            await _context.SaveChangesAsync();
        }

        // Clients
        if (!await _context.Clients.AnyAsync())
        {
            foreach (var client in Clients.ToList())
                await _context.Clients.AddAsync(client.ToEntity());
            await _context.SaveChangesAsync();
        }
    }
    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource("roles", "User roles", ["role"]), // Map 'role' claim to 'roles' scope
        new IdentityResource()
        {
            Name = "verification",
            UserClaims =
            [
                JwtClaimTypes.Email,
                JwtClaimTypes.EmailVerified
            ]
        }
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope(name: "file", displayName: "Files", userClaims: ["role"]),
        new ApiScope(name: "doc", displayName: "Docs")
    ];

    public static List<ApiResource> ApiResources =>
    [
        new ApiResource("webdir", "WebDirListing", [JwtClaimTypes.Email, JwtClaimTypes.Name])
        {
            Scopes = { "file" }
        },
        new ApiResource("documentDb", "DocumentDb", [JwtClaimTypes.Email, JwtClaimTypes.Name])
        {
            Scopes = { "doc" }
        }
    ];
    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "web",
            ClientSecrets = { new Secret("secret".Sha256()) },
            RequireClientSecret = false,
            AllowedGrantTypes = GrantTypes.Code,
            
            RedirectUris = {
                "http://localhost:5057/signin-oidc", "https://localhost:7028/signin-oidc",
                "http://localhost:8000/callback.html", "http://localhost:8000/popup.html", "http://localhost:8000/silentRenew.html",//localhost
                "https://docs.logout.work/callback.html", "https://docs.logout.work/popup.html", "https://docs.logout.work/silentRenew.html", // docs
                "https://med.logout.work/callback.html", "https://med.logout.work/popup.html", "https://med.logout.work/silentRenew.html", // media
                "https://fil.logout.work/signin-oidc", // files
                "https://user.logout.work/signin-oidc",
            },

            // where to redirect to after logout
            PostLogoutRedirectUris = {
                "https://localhost:8000", 
                "https://docs.logout.work",
                "https://med.logout.work",
                "https://fil.logout.work",
                "https://user.logout.work"
            },

            AllowedCorsOrigins = {
                "http://localhost:8000",
                "http://localhost:5057",
                "https://localhost:7028",
                "https://docs.logout.work",
                "https://med.logout.work",
                "https://fil.logout.work",
                "https://user.logout.work",

                "https://home.logout.work",
                "https://radarr.logout.work",
                "https://sonarr.logout.work",
                "https://prowlarr.logout.work",
                "https://deluge.logout.work"
            },

            AllowOfflineAccess = false,

            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                "verification",
                "roles",
                "doc",
                "file"
            }
        }
    ];
}