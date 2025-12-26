using System.Buffers.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AspAuth.Lib.Services;

public class WebAuthnService(ILogger<WebAuthnService> logger,
UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager)
{
    private readonly ILogger _logger = logger;
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
        var user = await _userManager.GetUserAsync(claimsUser) ?? throw new ApplicationException("no user");

        var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(credentialJson);

        if (!attestationResult.Succeeded)
        {
            _logger.LogError(attestationResult.Failure.Message);
            throw new ApplicationException(attestationResult.Failure.Message);   
        }
        
        var addResult = await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);

        if (!addResult.Succeeded)
        {
            foreach(var error in addResult.Errors)
                _logger.LogError($"{error.Code} - {error.Description}");
            throw new ApplicationException("Failed to store passkey");   
        }
    }

    public async Task<string> GetPasskeyRequestOptions(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ApplicationException("missing username");

        var user = await _userManager.FindByNameAsync(userName) ?? throw new ApplicationException("Can't find user");

        var optionsJson = await _signInManager.MakePasskeyRequestOptionsAsync(user);
        return optionsJson;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="claimsUser">logged in user</param>
    /// <param name="credentialId">base64 of byte[] credentialId</param>
    /// <returns></returns>
    public async Task PasskeyRename(ClaimsPrincipal claimsUser, string credentialId, string name)
    {
        try
        {
            var credIdBytes = Base64Url.DecodeFromChars(credentialId);    
        }
        catch (Exception e)
        {
            throw new ApplicationException("Error parsing keyId");
        }
    }
}