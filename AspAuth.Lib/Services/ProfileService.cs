using System.Security.Claims;
using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Services;

public class CustomProfileService : ProfileService<ApplicationUser>
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomProfileService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory) 
    : base(userManager, claimsFactory)
    {
        _context = context;
        _userManager = userManager;
    }

    public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        await base.GetProfileDataAsync(context);

        var user = await _userManager.GetUserAsync(context.Subject);
        if (user is null)
            return;

        var userP = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == user.Id);
        if (!(userP is null || userP.UserProfile is null || string.IsNullOrWhiteSpace(userP.UserProfile.FullName)))
        {
            context.IssuedClaims.Add(new Claim("fullname", userP.UserProfile.FullName));
        }
    }

    public override async Task IsActiveAsync(IsActiveContext context)
    {
        await base.IsActiveAsync(context);
    }
}