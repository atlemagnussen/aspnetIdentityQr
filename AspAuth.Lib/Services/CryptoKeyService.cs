using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Services;

public class CryptoKeyService
{
    private readonly ApplicationDbContext _dbContext;
    public CryptoKeyService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void CreateAndSaveNewKey()
    {
        var key = CreateKeys.CreateEcKey();

        var signingKey = new CryptoSigningKey
        {
            StartDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(2),
            Key = key
        };

        _dbContext.SigningKeys.Add(signingKey);
        _dbContext.SaveChanges();
    }

    public async Task<CryptoSigningKey> GetPrimaryKey()
    {
        var key = await _dbContext.SigningKeys.FirstOrDefaultAsync();
        if (key is null)
            throw new ApplicationException("No key!");
        return key;
    }
}