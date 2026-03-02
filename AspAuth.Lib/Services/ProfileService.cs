using System.Security.Claims;
using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Duende.IdentityModel;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Extensions;
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

        // Check the caller: context.Caller will be UserInfoEndpoint, IdentityToken, or AccessToken - ClaimsProviderAccessToken
        if (context.Caller == "ClaimsProviderAccessToken")
            return;
        
        string sub = context.Subject.GetSubjectId();

        var userP = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == sub);
        if (userP is null)
            return;

        if (!(userP.UserProfile is null || string.IsNullOrWhiteSpace(userP.UserProfile.FullName)))
            context.IssuedClaims.Add(new Claim("fullname", userP.UserProfile.FullName));
        
        var roles = await GetUserRolesAsync(userP);
        if (roles is not null && roles.Count > 0)
        {
            foreach (string role in roles)
                context.IssuedClaims.Add(new Claim(JwtClaimTypes.Role, role));
        }
    }

    private async Task<IReadOnlyList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        // Returns role names assigned to the user
        var roles = await _userManager.GetRolesAsync(user);
        return (IReadOnlyList<string>)roles;
    }
    public override async Task IsActiveAsync(IsActiveContext context)
    {
        await base.IsActiveAsync(context);
    }
}