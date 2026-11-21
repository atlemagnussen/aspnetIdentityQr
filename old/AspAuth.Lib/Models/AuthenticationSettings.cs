namespace AspAuth.Lib.Models;

public class AuthenticationSettings
{
    public bool EnableRegistration { get; init; }
    public AuthenticationClient Google { get; init; } = new() { ClientId = "", ClientSecret = "" };
}