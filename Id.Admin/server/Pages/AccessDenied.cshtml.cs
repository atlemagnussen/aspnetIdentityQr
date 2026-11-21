using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Id.Admin.Pages;

[Authorize]
public class AccessDeniedModel : PageModel
{
    public void OnGet() { }
}
