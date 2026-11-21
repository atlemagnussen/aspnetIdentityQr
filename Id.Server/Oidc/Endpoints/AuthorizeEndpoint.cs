using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Id.Lib.Oidc;
using Id.Server.Oidc.Clients;
using Id.Server.Oidc.Grants;
using Id.Server.Oidc.Resources;
using Id.Server.Oidc.Session;
using Id.Server.Oidc.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Id.Server.Oidc.Endpoints;

/// <summary>
/// Authorization endpoint: authorization code flow only (response_type=code), PKCE with S256.
/// Until client_id and redirect_uri are proven valid, errors go to our error page and never to the
/// redirect_uri (RFC 6749 4.1.2.1). After that, errors go back to the client.
/// </summary>
public static partial class AuthorizeEndpoint
{
    private const int MaxParameterLength = 2000;

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$")]
    private static partial Regex S256Challenge();

    public static async Task<IResult> HandleAsync(
        HttpContext http,
        ClientStore clients,
        ResourceStore resources,
        UserClaimsService users,
        GrantStore grants,
        TimeProvider time,
        IOptions<OidcServerOptions> options,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger(typeof(AuthorizeEndpoint));
        var opts = options.Value;

        var (p, parseError) = await ReadParametersAsync(http.Request);
        if (parseError is not null)
            return ErrorPage(log, opts, OAuthErrors.InvalidRequest, parseError);

        var client = await clients.FindEnabledAsync(p.GetValueOrDefault("client_id"));
        if (client is null)
            return ErrorPage(log, opts, OAuthErrors.UnauthorizedClient, $"Unknown or disabled client '{p.GetValueOrDefault("client_id")}'.");

        var redirectUri = p.GetValueOrDefault("redirect_uri");
        if (string.IsNullOrEmpty(redirectUri) || !client.RedirectUris.Any(r => string.Equals(r.RedirectUri, redirectUri, StringComparison.Ordinal)))
            return ErrorPage(log, opts, OAuthErrors.InvalidRequest, $"redirect_uri '{redirectUri}' isn't registered for client '{client.ClientId}'.");

        // From here on the client is known and the redirect_uri is trusted.
        var issuer = OidcIssuer.Get(http, opts);
        var state = p.GetValueOrDefault("state");
        var formPost = p.GetValueOrDefault("response_mode") switch
        {
            null or "query" => false,
            "form_post" => true,
            _ => (bool?)null,
        };
        IResult Fail(string error, string description)
        {
            log.LogInformation("Authorize request from {ClientId} rejected: {Error} {Description}", client.ClientId, error, description);
            return Respond(redirectUri, formPost ?? false, issuer, state, ("error", error), ("error_description", description));
        }

        if (formPost is null)
            return Fail(OAuthErrors.InvalidRequest, "Unsupported response_mode. Use query or form_post.");
        if (p.ContainsKey("request"))
            return Fail(OAuthErrors.RequestNotSupported, "Request objects aren't supported.");
        if (p.ContainsKey("request_uri"))
            return Fail(OAuthErrors.RequestUriNotSupported, "request_uri (PAR) isn't supported.");
        if (p.GetValueOrDefault("response_type") != "code")
            return Fail(OAuthErrors.UnsupportedResponseType, "Only response_type=code is supported.");
        if (!client.AllowedGrantTypes.Any(g => g.GrantType == GrantTypes.AuthorizationCode))
            return Fail(OAuthErrors.UnauthorizedClient, "Client isn't allowed the authorization code flow.");
        if (client.RequirePushedAuthorization || client.RequireRequestObject || client.RequireDPoP)
            return Fail(OAuthErrors.UnauthorizedClient, "Client requires PAR, request objects or DPoP, which this server doesn't support.");

        var (resolved, invalidScopes) = await resources.ResolveAsync(client, SplitScopes(p.GetValueOrDefault("scope")));
        if (resolved is null)
            return Fail(OAuthErrors.InvalidScope, string.IsNullOrEmpty(invalidScopes) ? "scope is required." : $"Invalid scope: {invalidScopes}");

        var challenge = p.GetValueOrDefault("code_challenge");
        var method = p.GetValueOrDefault("code_challenge_method");
        if (challenge is null)
        {
            if (client.RequirePkce)
                return Fail(OAuthErrors.InvalidRequest, "code_challenge is required.");
            if (method is not null)
                return Fail(OAuthErrors.InvalidRequest, "code_challenge_method without code_challenge.");
        }
        else
        {
            // plain is never accepted: it gives no protection when the request leaks.
            if (method != "S256")
                return Fail(OAuthErrors.InvalidRequest, "code_challenge_method must be S256.");
            if (!S256Challenge().IsMatch(challenge))
                return Fail(OAuthErrors.InvalidRequest, "Malformed code_challenge.");
        }

        var nonce = p.GetValueOrDefault("nonce");
        if (nonce is { Length: > 300 })
            return Fail(OAuthErrors.InvalidRequest, "nonce is too long.");

        var prompt = (p.GetValueOrDefault("prompt") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (prompt.Contains("none") && prompt.Count > 1)
            return Fail(OAuthErrors.InvalidRequest, "prompt=none can't be combined with other values.");

        long? maxAge = null;
        if (p.TryGetValue("max_age", out var maxAgeText))
        {
            if (!long.TryParse(maxAgeText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
                return Fail(OAuthErrors.InvalidRequest, "max_age must be a non-negative integer.");
            maxAge = parsed;
        }

        // ---- the user ----
        var session = SessionClaims.Read(await http.AuthenticateAsync(IdentityConstants.ApplicationScheme));
        var user = session is null ? null : await users.FindActiveAsync(session.SubjectId);
        var now = time.GetUtcNow().ToUnixTimeSeconds();

        var mustLogin = session is null || user is null
            || prompt.Contains("login")
            || (maxAge is { } age && now - session.AuthTime > age)
            || (session.Idp == SessionClaims.LocalIdp
                ? !client.EnableLocalLogin
                : client.IdentityProviderRestrictions.Count > 0 && client.IdentityProviderRestrictions.All(r => r.Provider != session.Idp));

        if (mustLogin)
        {
            if (prompt.Contains("none"))
                return Fail(OAuthErrors.LoginRequired, "The user must sign in.");

            // Back to this endpoint after login. prompt=login is dropped so the fresh login is accepted.
            var retry = p.Where(kv => kv.Key != "prompt").ToDictionary();
            prompt.Remove("login");
            if (prompt.Count > 0)
                retry["prompt"] = string.Join(' ', prompt);
            var returnUrl = QueryHelpers.AddQueryString(OidcPaths.Authorize, retry!);
            return Results.Redirect(QueryHelpers.AddQueryString(opts.LoginPath, "ReturnUrl", returnUrl));
        }

        // ---- issue the code ----
        var code = await grants.CreateAsync(GrantTypesStored.AuthorizationCode,
            new AuthorizationCodeData(client.ClientId, session!.SubjectId, session.SessionId, redirectUri,
                [.. resolved.Scopes], challenge, challenge is null ? null : method, nonce,
                session.AuthTime, session.Idp, session.Amr),
            client.ClientId, session.SubjectId, session.SessionId,
            time.GetUtcNow().UtcDateTime.AddSeconds(client.AuthorizationCodeLifetime));

        return Respond(redirectUri, formPost.Value, issuer, state, ("code", code), ("scope", string.Join(' ', resolved.Scopes)));
    }

    public static IEnumerable<string> SplitScopes(string? scope) =>
        (scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Query (GET) or form (POST) parameters. Repeated or oversized parameters are an error.</summary>
    private static async Task<(Dictionary<string, string> Parameters, string? Error)> ReadParametersAsync(HttpRequest request)
    {
        IEnumerable<KeyValuePair<string, StringValues>> source;
        if (HttpMethods.IsPost(request.Method))
        {
            if (!request.HasFormContentType)
                return ([], "POST requests must be form encoded.");
            source = await request.ReadFormAsync();
        }
        else
        {
            source = request.Query;
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, values) in source)
        {
            if (values.Count > 1)
                return ([], $"Parameter '{key}' is repeated.");
            var value = values.ToString();
            if (value.Length > MaxParameterLength)
                return ([], $"Parameter '{key}' is too long.");
            if (value.Length > 0)
                result[key] = value;
        }
        return (result, null);
    }

    /// <summary>Response to the client: redirect with a query string, or an auto-submitting form.</summary>
    private static IResult Respond(string redirectUri, bool formPost, string issuer, string? state, params (string Key, string Value)[] values)
    {
        var parameters = values.ToList();
        if (state is not null) parameters.Add(("state", state));
        parameters.Add(("iss", issuer)); // RFC 9207 mix-up protection

        if (!formPost)
            return Results.Redirect(QueryHelpers.AddQueryString(redirectUri,
                parameters.Select(kv => KeyValuePair.Create(kv.Key, (string?)kv.Value))));

        var enc = HtmlEncoder.Default;
        var inputs = string.Concat(parameters.Select(kv =>
            $"<input type=\"hidden\" name=\"{enc.Encode(kv.Key)}\" value=\"{enc.Encode(kv.Value)}\">"));
        var html = $"""
            <!doctype html><html><head><meta charset="utf-8"><title>Signing in</title></head>
            <body><form method="post" action="{enc.Encode(redirectUri)}">{inputs}<noscript><button type="submit">Continue</button></noscript></form>
            <script>document.forms[0].submit();</script></body></html>
            """;
        return new FormPostResult(html);
    }

    private static IResult ErrorPage(ILogger log, OidcServerOptions opts, string error, string description)
    {
        log.LogWarning("Authorize request rejected before redirect validation: {Error} {Description}", error, description);
        return Results.Redirect(QueryHelpers.AddQueryString(opts.ErrorPath, "error", error));
    }

    /// <summary>form_post page. The inline script needs its own CSP, and the page must not be cached or framed.</summary>
    private sealed class FormPostResult(string html) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            var headers = context.Response.Headers;
            headers.CacheControl = "no-store";
            headers.Pragma = "no-cache";
            headers["X-Frame-Options"] = "DENY";
            headers.ContentSecurityPolicy = "default-src 'none'; script-src 'sha256-8lDeP0UDwCO6/RhblgeH/ctdBzjVpJxrXizsnIk3cEQ='; form-action *; frame-ancestors 'none'";
            context.Response.ContentType = "text/html; charset=utf-8";
            return context.Response.WriteAsync(html);
        }
    }
}
