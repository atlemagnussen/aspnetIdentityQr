namespace AspAuth.Liv.Models;

public record AuthenticationClient
{
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
}