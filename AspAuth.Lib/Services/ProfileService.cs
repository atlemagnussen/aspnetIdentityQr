using System.Security.Claims;
using AspAuth.Lib.Models;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;

namespace AspAuth.Lib.Services;

public class CustomProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var user = await _userManager.GetUserAsync(context.Subject);
        if (user != null)
        {
            context.IssuedClaims.Add(new Claim("fullname", user.UserProfile?.FullName ?? ""));
        }
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        context.IsActive = true;
    }
}