using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Id.Lib.Data;
using Id.Lib.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Id.Server.Oidc.Grants;

public static class GrantTypesStored
{
    public const string AuthorizationCode = "authorization_code";
    public const string RefreshToken = "refresh_token";
}

public sealed record AuthorizationCodeData(
    string ClientId,
    string SubjectId,
    string? SessionId,
    string RedirectUri,
    string[] Scopes,
    string? CodeChallenge,
    string? CodeChallengeMethod,
    string? Nonce,
    long AuthTime,
    string Idp,
    string[] Amr);

public sealed record RefreshTokenData(
    string ClientId,
    string SubjectId,
    string? SessionId,
    string[] Scopes,
    long AuthTime,
    string Idp,
    string[] Amr,
    /// <summary>Creation of the first token in the chain; absolute lifetime counts from here.</summary>
    DateTime ChainCreated);

/// <summary>
/// Handles given to clients (codes, refresh tokens) are 256-bit random values. Only a hash of the
/// handle is stored, so a database leak doesn't leak usable handles.
/// </summary>
public class GrantStore(IdDbContext db, TimeProvider time)
{
    public async Task<string> CreateAsync<T>(string type, T data, string clientId, string? subjectId, string? sessionId, DateTime expiration)
    {
        var handle = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.PersistedGrants.Add(new PersistedGrant
        {
            Key = HashHandle(handle, type),
            Type = type,
            ClientId = clientId,
            SubjectId = subjectId,
            SessionId = sessionId,
            CreationTime = time.GetUtcNow().UtcDateTime,
            Expiration = expiration,
            Data = JsonSerializer.Serialize(data),
        });
        await db.SaveChangesAsync();
        return handle;
    }

    public async Task<(PersistedGrant Grant, T Data)?> FindAsync<T>(string type, string? handle)
    {
        if (string.IsNullOrEmpty(handle) || handle.Length > 100)
            return null;
        var key = HashHandle(handle, type);
        var grant = await db.PersistedGrants.AsNoTracking().SingleOrDefaultAsync(g => g.Key == key && g.Type == type);
        if (grant is null)
            return null;
        var data = JsonSerializer.Deserialize<T>(grant.Data);
        return data is null ? null : (grant, data);
    }

    /// <summary>Marks a grant used. Atomic: of two concurrent redemptions exactly one wins.</summary>
    public async Task<bool> TryConsumeAsync(long id)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var updated = await db.PersistedGrants
            .Where(g => g.Id == id && g.ConsumedTime == null)
            .ExecuteUpdateAsync(s => s.SetProperty(g => g.ConsumedTime, now));
        return updated == 1;
    }

    public Task SetExpirationAsync(long id, DateTime expiration) =>
        db.PersistedGrants.Where(g => g.Id == id).ExecuteUpdateAsync(s => s.SetProperty(g => g.Expiration, expiration));

    /// <summary>Revokes everything a client holds for a user session (used on code replay).</summary>
    public Task<int> RevokeAsync(string clientId, string subjectId, string? sessionId) =>
        db.PersistedGrants
            .Where(g => g.ClientId == clientId && g.SubjectId == subjectId && g.SessionId == sessionId
                        && g.Type == GrantTypesStored.RefreshToken)
            .ExecuteDeleteAsync();

    private static string HashHandle(string handle, string type) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes($"{handle}:{type}")));
}

/// <summary>Deletes expired grants every hour.</summary>
public class GrantCleanupService(IServiceScopeFactory scopes, TimeProvider time, ILogger<GrantCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1), time);
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IdDbContext>();
                var now = time.GetUtcNow().UtcDateTime;
                var deleted = await db.PersistedGrants.Where(g => g.Expiration < now).ExecuteDeleteAsync(stoppingToken);
                if (deleted > 0)
                    logger.LogInformation("Deleted {Count} expired grants", deleted);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Grant cleanup failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
