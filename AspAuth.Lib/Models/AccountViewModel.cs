namespace AspAuth.Lib.Models;

public class AccountViewModel
{
    public bool IsLoggedIn { get; set; }
    public string? UserName { get; set; }
    public string? Fullname { get; set; }
    public List<string> Claims { get; set; } = [];
}