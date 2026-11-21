using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UserAdmin.Pages;

[Authorize]
public class AccessDeniedModel : PageModel
{
    public void OnGet() { }
}
