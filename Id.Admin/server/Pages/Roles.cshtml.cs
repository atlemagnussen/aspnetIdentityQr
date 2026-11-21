using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Id.Admin.Pages;

[Authorize(Policies.RequiresAdmin)]
public class RolesModel : PageModel
{
    public void OnGet() { }
}
