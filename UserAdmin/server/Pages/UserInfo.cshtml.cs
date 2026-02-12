using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserAdmin.Pages;

[Authorize]
public class UserInfoModel : PageModel
{
    public AccountViewModel View { get; set; } = default!;

    public IActionResult OnGet()
    {
        View = new AccountViewModel { IsLoggedIn = false };

        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            View.IsLoggedIn = true;
            View.UserName = User.Identity.Name;
            View.Claims = new List<string>();

            var claims = User.Claims.ToList();

            if (claims.Count > 0)
            {
                foreach (var claim in claims)
                    View.Claims.Add($"{claim.Type} = {claim.Value}");
            }
        }

        return Page();
    }
}
public class AccountViewModel
{
    public bool IsLoggedIn { get; set; }
    public string? UserName { get; set; }
    public List<string> Claims { get; set; } = [];
}