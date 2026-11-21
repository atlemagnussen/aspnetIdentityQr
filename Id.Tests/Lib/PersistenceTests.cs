using Id.Lib.Entities;
using Id.Lib.Services;
using Id.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Id.Tests.Lib;

public class PersistenceTests(PostgresFixture pg)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Client_with_children_round_trips_and_cascades_on_delete()
    {
        var clientId = $"test-{Guid.NewGuid():N}";
        await using (var db = pg.NewContext())
        {
            db.Clients.Add(new Client
            {
                ClientId = clientId,
                RefreshTokenUsage = TokenUsage.ReUse,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                AccessTokenType = AccessTokenType.Reference,
                AllowedGrantTypes = [new() { GrantType = "authorization_code" }],
                RedirectUris = [new() { RedirectUri = "https://app.example/signin-oidc" }],
                AllowedScopes = [new() { Scope = "openid" }, new() { Scope = "profile" }],
                ClientSecrets = [new() { Value = "secret".Sha256() }],
                Claims = [new() { Type = "tenant", Value = "a" }],
                Properties = [new() { Key = "k", Value = "v" }],
            });
            await db.SaveChangesAsync(Ct);
        }

        await using (var db = pg.NewContext())
        {
            var c = await db.Clients
                .Include(x => x.AllowedGrantTypes).Include(x => x.RedirectUris).Include(x => x.AllowedScopes)
                .Include(x => x.ClientSecrets).Include(x => x.Claims).Include(x => x.Properties)
                .AsSplitQuery()
                .SingleAsync(x => x.ClientId == clientId, Ct);

            Assert.Equal(TokenUsage.ReUse, c.RefreshTokenUsage);
            Assert.Equal(TokenExpiration.Sliding, c.RefreshTokenExpiration);
            Assert.Equal(AccessTokenType.Reference, c.AccessTokenType);
            Assert.Equal(TimeSpan.FromMinutes(5), c.DPoPClockSkew);
            Assert.Equal(["openid", "profile"], c.AllowedScopes.Select(s => s.Scope).Order());
            Assert.Equal(SecretTypes.SharedSecret, Assert.Single(c.ClientSecrets).Type);

            db.Clients.Remove(c);
            await db.SaveChangesAsync(Ct);
        }

        await using (var db = pg.NewContext())
        {
            Assert.Equal(0, await db.Set<ClientScope>().CountAsync(s => s.Client.ClientId == clientId, Ct));
        }
    }

    [Fact]
    public async Task Signing_key_jwk_round_trips_through_jsonb_with_private_part()
    {
        using var ec = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP521);
        var jwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(new ECDsaSecurityKey(ec) { KeyId = Guid.NewGuid().ToString("N") });

        int id;
        await using (var db = pg.NewContext())
        {
            var key = new CryptoSigningKey { StartDate = DateTime.UtcNow, ExpiryDate = DateTime.UtcNow.AddYears(1), Key = jwk };
            db.SigningKeys.Add(key);
            await db.SaveChangesAsync(Ct);
            id = key.Id;
        }

        await using (var db = pg.NewContext())
        {
            var loaded = await db.SigningKeys.SingleAsync(k => k.Id == id, Ct);
            Assert.Equal(jwk.KeyId, loaded.Key.KeyId);
            Assert.Equal(jwk.Crv, loaded.Key.Crv);
            Assert.Equal(jwk.D, loaded.Key.D);
            Assert.True(loaded.Key.HasPrivateKey);
        }
    }
}
