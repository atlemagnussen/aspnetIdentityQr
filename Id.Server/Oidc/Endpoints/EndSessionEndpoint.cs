using Id.Lib.Entities;
using Id.Lib.Oidc;
using Id.Server.Oidc.Clients;
using Id.Server.Oidc.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Id.Server.Oidc.Endpoints;

/// <summary>
/// RP-initiated logout (OIDC RP-Initiated Logout 1.0). Signs the user out of the ID server, then returns
/// to post_logout_redirect_uri only when it's registered for the client named by id_token_hint or client_id.
/// No front- or back-channel notification of other clients yet.
/// </summary>
public static class EndSessionEndpoint
{
    public static async Task<IResult> HandleAsync(
        HttpContext http,
        TokenValidator validator,
        ClientStore clients,
        SignInManager<ApplicationUser> signIn,
        IOptions<OidcServerOptions> options,
        ILoggerFactory loggerFactory)
    {
        var log = loggerFactory.CreateLogger(typeof(EndSessionEndpoint));
        var opts = options.Value;
        var p = HttpMethods.IsPost(http.Request.Method) && http.Request.HasFormContentType
            ? (await http.Request.ReadFormAsync()).ToDictionary(kv => kv.Key, kv => kv.Value.ToString())
            : http.Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        string? hintClientId = null, hintSubject = null;
        if (p.TryGetValue("id_token_hint", out var hint) && hint.Length > 0)
        {
            var identity = await validator.ValidateIdTokenHintAsync(OidcIssuer.Get(http, opts), hint);
            if (identity is null)
                return Results.Redirect(QueryHelpers.AddQueryString(opts.ErrorPath, "error", OAuthErrors.InvalidRequest));
            hintClientId = identity.FindFirst("aud")?.Value;
            hintSubject = identity.FindFirst(JwtClaimTypes.Subject)?.Value;
        }

        var clientId = p.GetValueOrDefault("client_id") is { Length: > 0 } cid ? cid : hintClientId;
        if (hintClientId is not null && clientId != hintClientId)
            return Results.Redirect(QueryHelpers.AddQueryString(opts.ErrorPath, "error", OAuthErrors.InvalidRequest));

        string? returnTo = null;
        if (p.GetValueOrDefault("post_logout_redirect_uri") is { Length: > 0 } postLogout)
        {
            var client = await clients.FindEnabledAsync(clientId);
            if (client is null || !client.PostLogoutRedirectUris.Any(u => string.Equals(u.PostLogoutRedirectUri, postLogout, StringComparison.Ordinal)))
            {
                log.LogWarning("End session with unregistered post_logout_redirect_uri {Uri} for client {ClientId}", postLogout, clientId);
                return Results.Redirect(QueryHelpers.AddQueryString(opts.ErrorPath, "error", OAuthErrors.InvalidRequest));
            }
            returnTo = p.GetValueOrDefault("state") is { Length: > 0 } state
                ? QueryHelpers.AddQueryString(postLogout, "state", state)
                : postLogout;
        }

        // A hint for another user doesn't end this user's session.
        var current = http.User.FindFirst(JwtClaimTypes.Subject)?.Value
            ?? (await http.AuthenticateAsync(IdentityConstants.ApplicationScheme)).Principal?.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (current is not null && (hintSubject is null || hintSubject == current))
            await signIn.SignOutAsync();

        return Results.Redirect(returnTo ?? opts.LoggedOutPath);
    }
}
