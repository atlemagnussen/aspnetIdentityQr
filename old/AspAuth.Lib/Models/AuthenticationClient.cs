namespace AspAuth.Lib.Models;

public record AuthenticationClient
{
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? TenantId { get; init; }
    public string? CallbackPath { get; init; }
    public string? SignedOutCallbackPath { get; init; }
}