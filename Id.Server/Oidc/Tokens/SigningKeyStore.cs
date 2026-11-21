using Id.Lib.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Id.Server.Oidc.Tokens;

public sealed record SigningKey(JsonWebKey Key, string Algorithm)
{
    public SigningCredentials Credentials => new(Key, Algorithm);
}

/// <summary>
/// Signing keys from the SigningKeys table (private JWKs). The newest key that is valid now signs;
/// every unexpired key is published for validation. Cached briefly, so a new key shows up within minutes.
/// A JWK without "kid" stays without one: that is what the old server issued, and existing tokens
/// and resource servers expect it.
/// </summary>
public class SigningKeyStore(IServiceScopeFactory scopes, IMemoryCache cache, TimeProvider time)
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);

    public async Task<SigningKey> GetSigningKeyAsync()
    {
        var now = time.GetUtcNow().UtcDateTime;
        var keys = await LoadAsync();
        return keys.Where(k => k.StartDate <= now && k.ExpiryDate > now).OrderByDescending(k => k.StartDate)
            .Select(k => k.Key).FirstOrDefault()
            ?? throw new InvalidOperationException("No valid signing key in SigningKeys.");
    }

    public async Task<IReadOnlyList<SigningKey>> GetValidationKeysAsync()
    {
        var now = time.GetUtcNow().UtcDateTime;
        return [.. (await LoadAsync()).Where(k => k.ExpiryDate > now).Select(k => k.Key)];
    }

    private async Task<List<(DateTime StartDate, DateTime ExpiryDate, SigningKey Key)>> LoadAsync() =>
        (await cache.GetOrCreateAsync("oidc:signing-keys", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdDbContext>();
            var rows = await db.SigningKeys.AsNoTracking().ToListAsync();
            return rows.Select(r => (r.StartDate, r.ExpiryDate, new SigningKey(r.Key, AlgorithmFor(r.Key)))).ToList();
        }))!;

    public static string AlgorithmFor(JsonWebKey key) =>
        !string.IsNullOrEmpty(key.Alg) ? key.Alg
        : key.Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve ? key.Crv switch
        {
            JsonWebKeyECTypes.P256 => SecurityAlgorithms.EcdsaSha256,
            JsonWebKeyECTypes.P384 => SecurityAlgorithms.EcdsaSha384,
            JsonWebKeyECTypes.P521 => SecurityAlgorithms.EcdsaSha512,
            _ => throw new NotSupportedException($"Unsupported curve {key.Crv}"),
        }
        : key.Kty == JsonWebAlgorithmsKeyTypes.RSA ? SecurityAlgorithms.RsaSha256
        : throw new NotSupportedException($"Unsupported key type {key.Kty}");

    /// <summary>Public part of a key for the JWKS document.</summary>
    public static Dictionary<string, object> PublicJwk(SigningKey key)
    {
        var k = key.Key;
        var jwk = new Dictionary<string, object> { ["kty"] = k.Kty, ["use"] = "sig", ["alg"] = key.Algorithm };
        if (!string.IsNullOrEmpty(k.Kid)) jwk["kid"] = k.Kid;
        if (k.Kty == JsonWebAlgorithmsKeyTypes.EllipticCurve)
        {
            jwk["crv"] = k.Crv;
            jwk["x"] = k.X;
            jwk["y"] = k.Y;
        }
        else
        {
            jwk["n"] = k.N;
            jwk["e"] = k.E;
        }
        return jwk;
    }
}
