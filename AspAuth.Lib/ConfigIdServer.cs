using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        [
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
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
            new ApiScope(name: "api1", displayName: "My API")
        ];

    public static List<ApiResource> ApiResources =>
            new List<ApiResource>
            {
                new ApiResource("webdir", "WebDirListing", [JwtClaimTypes.Email, JwtClaimTypes.Name])
                {
                    Scopes = { "api1" }
                }

            };
    public static IEnumerable<Client> Clients =>
        [
            //new Client
            //{
            //    ClientId = "client",

                // no interactive user, use the clientid/secret for authentication
            //    AllowedGrantTypes = GrantTypes.ClientCredentials,

                // secret for authentication
            //    ClientSecrets =
            //    {
            //        new Secret("secret".Sha256())
            //    },

                // scopes that client has access to
            //    AllowedScopes = { "api1" }
            //},
            // interactive ASP.NET Core Web App
            new Client
            {
                ClientId = "web",
                ClientSecrets = { new Secret("secret".Sha256()) },
                RequireClientSecret = false,
                AllowedGrantTypes = GrantTypes.Code,
                
                // where to redirect to after login
                RedirectUris = { "https://localhost:8000/signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { "https://localhost:8000/signout-callback-oidc" },

                AllowOfflineAccess = false,

                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "verification",
                    "api1"
                }
            }
        ];
}