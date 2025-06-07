using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace AspAuth.Lib.Services;

public static class CreateKeys
{
    public static JsonWebKey CreateEcKey()
    {
        using ECDsa ecd = ECDsa.Create(ECCurve.NamedCurves.nistP521);

        var privateKeyParams = ecd.ExportParameters(true);

        ECDsaSecurityKey privateKey = new ECDsaSecurityKey(ecd);
        
        var jwkPrivate = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(privateKey);
        return jwkPrivate;
    }
}