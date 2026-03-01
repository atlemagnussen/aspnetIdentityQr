using System.Security.Claims;
using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspAuth.Lib.Services;

public class CustomProfileService : ProfileService<ApplicationUser>
{
    private readonly ApplicationDbContext _context;

    public CustomProfileService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory) 
    : base(userManager, claimsFactory)
    {
        _context = context;
    }

    public override async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        await base.GetProfileDataAsync(context);

        string sub = context.Subject.GetSubjectId();

        var userP = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == sub);
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