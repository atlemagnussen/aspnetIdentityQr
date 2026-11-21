using Id.Lib.Data;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Id.Server.Oidc;

/// <summary>
/// CORS for browser clients (SPAs) on the protocol endpoints they call with fetch: discovery, JWKS,
/// token and userinfo. An origin is allowed when any client lists it in ClientCorsOrigins.
/// Every other path gets no CORS policy.
/// </summary>
public class OidcCorsPolicyProvider(IServiceScopeFactory scopes) : ICorsPolicyProvider
{
    private static readonly string[] Paths = [OidcPaths.Discovery, OidcPaths.Jwks, OidcPaths.Token, OidcPaths.UserInfo];

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (origin.Length == 0 || !Paths.Any(p => context.Request.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
            return null;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdDbContext>();
        var lower = origin.ToLowerInvariant();
        var allowed = await db.Set<Id.Lib.Entities.ClientCorsOrigin>().AnyAsync(o => o.Origin.ToLower() == lower && o.Client.Enabled);
        if (!allowed)
            return null;

        return new CorsPolicyBuilder()
            .WithOrigins(origin)
            .WithMethods("GET", "POST")
            .WithHeaders("Authorization", "Content-Type")
            .SetPreflightMaxAge(TimeSpan.FromHours(1))
            .Build();
    }
}
