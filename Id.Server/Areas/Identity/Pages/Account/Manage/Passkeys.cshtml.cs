
using System.Buffers.Text;
using Id.Lib.Entities;
using Id.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Id.Server.Areas.Identity.Pages.Account.Manage;
public class PasskeysModel(UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public List<PassKeyViewModel> Keys = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is not null)
        {
            var keys = await _userManager.GetPasskeysAsync(user);
            foreach(var key in keys)
            {
                Keys.Add(new PassKeyViewModel
                {
                    CredentialId = Base64Url.EncodeToString(key.CredentialId),
                    Name = key.Name ?? "unnamed"
                });
            }
        }
        return Page();
    }
}