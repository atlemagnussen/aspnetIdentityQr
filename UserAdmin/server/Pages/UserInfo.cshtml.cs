using AspAuth.Lib.Data;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace UserAdmin.Pages;

[Authorize]
public class UserInfoModel : PageModel
{
    private readonly ApplicationDbContext _context;
    public UserInfoModel(ApplicationDbContext applicationDbContext)
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
            View.Claims = new List<string>();

            var claims = User.Claims.ToList();

            if (claims.Count > 0)
            {
                foreach (var claim in claims)
                    View.Claims.Add($"{claim.Type} = {claim.Value}");
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
}
public class AccountViewModel
{
    public bool IsLoggedIn { get; set; }
    public string? UserName { get; set; }
    public string? Fullname { get; set; }
    public List<string> Claims { get; set; } = [];
}