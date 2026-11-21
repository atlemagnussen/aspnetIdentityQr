using Duende.IdentityServer.Stores;
using Microsoft.IdentityModel.Tokens;

namespace AspAuth.Lib.Services;

public class LocalSigningCredentialStore : ISigningCredentialStore
{
    private readonly CryptoKeyService _service;

    public LocalSigningCredentialStore(CryptoKeyService service)
    {
        _service = service;
    }

    public async Task<SigningCredentials> GetSigningCredentialsAsync()
    {
        var key = await _service.GetPrimaryKey();

        var keyCred = new SigningCredentials(key.Key, SecurityAlgorithms.EcdsaSha512);

        return keyCred;
    }
}