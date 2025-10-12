using System.Security.Claims;

namespace AspAuth.Lib.Services;
public static class ClaimsHelper
{
    /// <summary>
    /// Will try the first claim name and then all the way to the last
    /// </summary>
    public static string? GetValueByType(this IEnumerable<Claim> claims, params string[] types)
    {

        foreach (var type in types)
        {
            var claimOfType = claims.FirstOrDefault(c => c.Type == type);
            if (claimOfType != null)
            {
                return claimOfType.Value;
            }
        }
        return null;
    }
}