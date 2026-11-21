using System.Security.Claims;
using Duende.IdentityModel;

namespace UserAdmin.Auth;

public static class CurrentUser
{
    /// <summary>Subject id of the signed-in admin (raw "sub" claim; MapInboundClaims is off).</summary>
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtClaimTypes.Subject) ?? throw new System.Security.SecurityException("No subject claim");
}
