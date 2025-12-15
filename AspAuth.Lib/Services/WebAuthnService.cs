using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace AspAuth.Lib.Services;

public class WebAuthnService(UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager)
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly SignInManager<IdentityUser> _signInManager = signInManager;

    public async Task<string?> PasskeyCreationOptions(ClaimsPrincipal claimsUser)
    {
        var user = await _userManager.GetUserAsync(claimsUser);

        if (user is null)
            return null;

        var optionsJson = await _signInManager.MakePasskeyCreationOptionsAsync(new()
        {
            Id = user.Id,
            Name = user.UserName ?? "user",
            DisplayName = user.UserName ?? "User"
        });

        return optionsJson;
    }

    public async Task PasskeyCreate(ClaimsPrincipal claimsUser, string credentialJson)
    {
        var user = await _userManager.GetUserAsync(claimsUser);
        if (user is null)
            throw new ApplicationException("no user");

        var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(credentialJson);

        if (!attestationResult.Succeeded)
            throw new ApplicationException(attestationResult.Failure.Message);
        
        var addResult = await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);

        if (!addResult.Succeeded)
            throw new ApplicationException("Failed to store passkey");
    }
}