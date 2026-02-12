using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserAdmin.Pages;

[Authorize]
public class ManagerModel : PageModel
{
    public string? LoggedInUserName { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            LoggedInUserName = User.Identity.Name;
        }

        return Page();
    }
}