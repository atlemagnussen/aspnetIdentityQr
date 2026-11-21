namespace Id.Server.Oidc;

/// <summary>Config section "Oidc".</summary>
public class OidcServerOptions
{
    /// <summary>
    /// Fixed issuer, e.g. "https://id.logout.work". Empty: derived from the request (scheme + host,
    /// lower case), which is what Duende did. Behind the proxy that gives the public URL.
    /// </summary>
    public string? IssuerUri { get; set; }

    public string LoginPath { get; set; } = "/Identity/Account/Login";
    public string ErrorPath { get; set; } = "/Error";
    public string LoggedOutPath { get; set; } = "/Identity/Account/Logout";
}

public static class OidcPaths
{
    public const string Discovery = "/.well-known/openid-configuration";
    public const string Jwks = "/.well-known/openid-configuration/jwks";
    public const string Authorize = "/connect/authorize";
    public const string Token = "/connect/token";
    public const string UserInfo = "/connect/userinfo";
    public const string EndSession = "/connect/endsession";
}

public static class OidcIssuer
{
    public static string Get(HttpContext context, OidcServerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.IssuerUri))
            return options.IssuerUri.TrimEnd('/');
        var request = context.Request;
        return $"{request.Scheme}://{request.Host.Value}{request.PathBase.Value}".TrimEnd('/').ToLowerInvariant();
    }

    /// <summary>The static audience every access token carries (Duende's EmitStaticAudienceClaim).</summary>
    public static string StaticAudience(string issuer) => issuer + "/resources";
}
