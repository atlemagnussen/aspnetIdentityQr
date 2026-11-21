using System.Security.Cryptography;
using System.Text;

namespace Id.Lib.Services;

public static class Hashing
{
    /// <summary>
    /// base64(SHA-256(UTF-8(value))). The format Duende used for client secrets, so existing
    /// hashes in ClientSecrets keep validating. Empty input gives an empty string.
    /// </summary>
    public static string Sha256(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
