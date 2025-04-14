namespace AspAuth.Liv.Models;

public record EmailSettings
{
    public string Server { get; init; }  = string.Empty;
    public int Port { get; init; }
    public string From { get; init; } = string.Empty;
    public string Password { get; init; }  = string.Empty;
}