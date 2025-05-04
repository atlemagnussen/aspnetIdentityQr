namespace AspAuth.Lib.Models;

public record AuthenticationClient
{
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
}