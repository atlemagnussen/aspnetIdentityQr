using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UserAdmin.Api.Users.DataService;

namespace UserAdmin.Pages;

[Authorize]
public class UserInfoModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserDataService _userDataService;
    public UserInfoModel(ApplicationDbContext applicationDbContext,
        UserDataService userDataService)
    {
        _context = applicationDbContext;
        _userDataService = userDataService;
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
                    if (claim.Type == "role" && claim.Value.Equals(UserRoles.Admin.ToString()))
                        View.IsAdmin = true;
                    View.Claims.Add($"{claim.Type} = {claim.Value}");
                }
            }

            var userId = User.Identity.GetSubjectId();
            if (userId is not null)
            {
                var user = await _context.Users.Include(u => u.UserProfile).SingleOrDefaultAsync(u => u.Id == userId);
                
                // if (user is not null)
                // {
                //     if (user.UserProfile is null)
                //     {
                //         user.UserProfile = new AspAuth.Lib.Models.UserProfile
                //         {
                //             User = user,
                //             UserId = userId,
                //             FullName = "Atle Magnussen"
                //         };
                //         await _context.SaveChangesAsync();
                //     }    
                // }

                View.Fullname = user?.UserProfile?.FullName;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMakeAdminAsync()
    {
        var userId = User.Identity?.GetSubjectId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToPage();
        }

        await _userDataService.AddRole(userId, UserRoles.Admin);
        return RedirectToPage();
    }
}