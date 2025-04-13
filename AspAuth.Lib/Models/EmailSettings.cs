namespace AspAuth.Liv.Models;

public record EmailSettings
{
    public string Server { get; init; }
    public int Port { get; init; }
    public string From { get; init; }
    public string Password { get; init;}
}