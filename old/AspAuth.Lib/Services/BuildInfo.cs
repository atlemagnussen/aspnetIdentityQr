using System.Globalization;
using System.Reflection;

namespace AspAuth.Lib.Services;

/// <summary>
/// Version/commit/build time stamped into the entry assembly by CI (see Directory.Build.props).
/// Local builds fall back to the SDK's InformationalVersion ("1.0.0+&lt;sha&gt;").
/// </summary>
public sealed record BuildInfo(string Version, string? Commit, DateTimeOffset? BuildTime)
{
    public static BuildInfo Current { get; } = Load(Assembly.GetEntryAssembly() ?? typeof(BuildInfo).Assembly);

    /// <summary>e.g. "dev-abc1234 · 2026-11-21 14:03 UTC" or "v1.2.0 (abc1234) · ..."</summary>
    public string Display
    {
        get
        {
            var text = Version;
            if (!string.IsNullOrEmpty(Commit) && !Version.Contains(Commit, StringComparison.OrdinalIgnoreCase))
                text += $" ({Commit})";
            if (BuildTime is { } time)
                text += $" · {time.UtcDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} UTC";
            return text;
        }
    }

    private static BuildInfo Load(Assembly assembly)
    {
        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .ToDictionary(a => a.Key, a => a.Value!);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var sourceRevision = informational?.Split('+', 2) is [_, var rev] ? rev : null;

        var version = metadata.GetValueOrDefault("BuildVersion") ?? "local";
        var commit = Shorten(metadata.GetValueOrDefault("BuildCommit") ?? sourceRevision);

        DateTimeOffset? buildTime = DateTimeOffset.TryParse(metadata.GetValueOrDefault("BuildTime"),
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed : null;

        return new BuildInfo(version, commit, buildTime);
    }

    private static string? Shorten(string? sha) =>
        sha is { Length: > 7 } ? sha[..7] : sha;
}
