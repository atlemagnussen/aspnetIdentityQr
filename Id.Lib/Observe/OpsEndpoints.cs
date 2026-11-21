using Id.Lib.Data;
using Id.Lib.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Id.Lib.Observe;

/// <summary>
/// Kubernetes-style operational endpoints, anonymous and shared by both apps:
///   /livez   - liveness: the process is up and serving requests. No dependencies are checked,
///              so a database outage doesn't make Kubernetes restart every pod.
///   /readyz  - readiness: can we serve real traffic? Includes the database.
///   /version - build stamp as JSON (see <see cref="BuildInfo"/>).
/// All are short-circuited at UseRouting, so auth/IdentityServer middleware never runs for them.
/// That matters: the ID server's auth middleware loads Data Protection keys from Postgres, which
/// would otherwise make /livez fail (and Kubernetes restart pods) whenever the DB is down.
/// </summary>
public static class OpsEndpoints
{
    public const string Live = "/livez";
    public const string Ready = "/readyz";
    public const string Version = "/version";

    private const string ReadyTag = "ready";

    public static bool IsOpsPath(PathString path) =>
        path.StartsWithSegments(Live) || path.StartsWithSegments(Ready) || path.StartsWithSegments(Version);

    /// <summary>Requires IdDbContext to be registered.</summary>
    public static IServiceCollection AddOpsHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<IdDbContext>("database", tags: [ReadyTag]);
        return services;
    }

    public static IEndpointRouteBuilder MapOpsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(Live, new HealthCheckOptions { Predicate = _ => false })
            .ShortCircuit();

        endpoints.MapHealthChecks(Ready, new HealthCheckOptions { Predicate = c => c.Tags.Contains(ReadyTag) })
            .ShortCircuit();

        endpoints.MapGet(Version, () =>
            {
                var info = BuildInfo.Current;
                return Results.Ok(new { info.Version, info.Commit, info.BuildTime });
            })
            .ShortCircuit()
            .ExcludeFromDescription();

        return endpoints;
    }
}
