using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.IdentityModel.Tokens;

namespace AspAuth.Lib.Services;

public class LocalValidationKeysStore : IValidationKeysStore
{
    private readonly CryptoKeyService _service;

    public LocalValidationKeysStore(CryptoKeyService service)
    {
        _service = service;
    }

    public async Task<IEnumerable<SecurityKeyInfo>> GetValidationKeysAsync()
    {
        var key = await _service.GetPrimaryKey();

        var keyInfo = new SecurityKeyInfo
        {
            Key = key.Key,
            SigningAlgorithm = SecurityAlgorithms.EcdsaSha512
        };

        return [keyInfo];
    }
}