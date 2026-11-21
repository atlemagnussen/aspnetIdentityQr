using Id.Lib.Data;
using Id.Lib.Entities;
using System.Security.Claims;
using Id.Lib.Oidc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Id.Admin.Pages;

[Authorize]
public class UserInfoModel : PageModel
{
    private readonly IdDbContext _context;
    public UserInfoModel(IdDbContext applicationDbContext)
    {
        _context = applicationDbContext;
    }
    public AccountViewModel View { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        View = new AccountViewModel { IsLoggedIn = false };

        if (User.Identity is not null && User.Identity.IsAuthenticated)
        {
            View.IsLoggedIn = true;
            View.UserName = User.Identity.Name;
            View.Claims = [];

            var claims = User.Claims.ToList();

            if (claims.Count > 0)
            {
                foreach (var claim in claims) {
                    if (claim.Type == JwtClaimTypes.Role && claim.Value.Equals(UserRoles.Admin.ToString()))
                        View.IsAdmin = true;
                    View.Claims.Add($"{claim.Type} = {claim.Value}");
                }
            }

            var userId = User.FindFirstValue(JwtClaimTypes.Subject);
            if (userId is not null)
            {
                var user = await _context.Users.Include(u => u.UserProfile).SingleOrDefaultAsync(u => u.Id == userId);
                View.Fullname = user?.UserProfile?.FullName;
            }
        }

        return Page();
    }
}
public class AccountViewModel
{
    public bool IsLoggedIn { get; set; }
    public string? UserName { get; set; }
    public string? Fullname { get; set; }
    public List<string> Claims { get; set; } = [];
    public bool IsAdmin { get; set; }
}
