using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Id.Lib.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Id.Server.Oidc.Session;

/// <summary>
/// The login session as the protocol sees it: session id (sid), auth_time, idp and amr, carried in the
/// Identity application cookie. Shapes match what Duende wrote, so cookies from before the switch work:
/// sid in the "session_id" authentication property, the rest as claims.
/// </summary>
public static class SessionClaims
{
    public const string SessionIdItem = "session_id";
    public const string LocalIdp = "local";

    /// <summary>Cookie OnSigningIn: stamp a new login with session data.</summary>
    public static Task AddOnSignIn(CookieSigningInContext context)
    {
        if (context.Principal?.Identity is not ClaimsIdentity identity)
            return Task.CompletedTask;

        // External logins carry the provider scheme as the authentication method.
        var external = identity.FindFirst(ClaimTypes.AuthenticationMethod)?.Value;

        if (!identity.HasClaim(c => c.Type == JwtClaimTypes.AuthenticationTime))
            identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationTime,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64));
        if (!identity.HasClaim(c => c.Type == JwtClaimTypes.IdentityProvider))
            identity.AddClaim(new Claim(JwtClaimTypes.IdentityProvider, external ?? LocalIdp));
        if (!identity.HasClaim(c => c.Type == JwtClaimTypes.AuthenticationMethod))
            identity.AddClaim(new Claim(JwtClaimTypes.AuthenticationMethod, external is null ? "pwd" : "external"));

        if (!context.Properties.Items.ContainsKey(SessionIdItem))
            context.Properties.Items[SessionIdItem] = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        return Task.CompletedTask;
    }

    /// <summary>Security stamp refresh rebuilds the principal; carry the session claims over.</summary>
    public static Task CopyOnRefresh(SecurityStampRefreshingPrincipalContext context)
    {
        if (context.NewPrincipal?.Identity is not ClaimsIdentity target || context.CurrentPrincipal is null)
            return Task.CompletedTask;

        foreach (var type in new[] { JwtClaimTypes.AuthenticationTime, JwtClaimTypes.IdentityProvider, JwtClaimTypes.AuthenticationMethod })
        {
            if (target.HasClaim(c => c.Type == type))
                continue;
            foreach (var claim in context.CurrentPrincipal.FindAll(type))
                target.AddClaim(new Claim(claim.Type, claim.Value, claim.ValueType));
        }
        return Task.CompletedTask;
    }

    public static UserSession? Read(AuthenticateResult result)
    {
        var principal = result.Principal;
        var sub = principal?.FindFirstValue(JwtClaimTypes.Subject);
        if (!result.Succeeded || principal is null || string.IsNullOrEmpty(sub))
            return null;

        long.TryParse(principal.FindFirstValue(JwtClaimTypes.AuthenticationTime), CultureInfo.InvariantCulture, out var authTime);
        var amr = principal.FindAll(JwtClaimTypes.AuthenticationMethod).Select(c => c.Value).Distinct().ToArray();
        return new UserSession(
            SubjectId: sub,
            SessionId: result.Properties?.Items.TryGetValue(SessionIdItem, out var sid) == true ? sid : null,
            AuthTime: authTime,
            Idp: principal.FindFirstValue(JwtClaimTypes.IdentityProvider) ?? LocalIdp,
            Amr: amr.Length > 0 ? amr : ["pwd"]);
    }
}

public sealed record UserSession(string SubjectId, string? SessionId, long AuthTime, string Idp, string[] Amr);
