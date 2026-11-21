using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.IdentityModel.Tokens;

namespace AspAuth.Lib.Models;

public record CryptoSigningKey
{
    [Key]
    public int Id { get; set; }
    public DateTime StartDate { get; init; }
    public DateTime ExpiryDate { get; init; }

    [Column(TypeName = "jsonb")]
    public required JsonWebKey Key { get; init; }
}