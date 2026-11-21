using System.Buffers.Text;
using System.Security.Claims;
using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AspAuth.Lib.Services;

public class WebAuthnService(ILogger<WebAuthnService> logger,
UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
{
    private readonly ILogger _logger = logger;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

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

    /// <summary>
    /// Pre login with no authenticated user
    /// </summary>
    /// <param name="userName">userName that wants to authenticate with passkey or blank for conditional mediation</param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    public async Task<string> GetPasskeyRequestOptions(string? userName)
    {
        ApplicationUser? user = null;
        if (userName is not null)
            user = await _userManager.FindByNameAsync(userName);

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
        var user = await _userManager.GetUserAsync(claimsUser) ?? throw new ApplicationException("no user");

        byte[] credIdBytes;
        try
        {
            credIdBytes = Base64Url.DecodeFromChars(credentialId);    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing key");
            throw new ApplicationException("Error parsing keyId");
        }

        var passkey = await _userManager.GetPasskeyAsync(user, credIdBytes) ?? throw new ApplicationException("Could not find key");
        passkey.Name = name;

        var result = await _userManager.AddOrUpdatePasskeyAsync(user, passkey);
        if (!result.Succeeded)
        {
            var errors = string.Join(',', result.Errors);
            throw new ApplicationException($"Could not update passkey: {errors}");
        }
    }

    public async Task PasskeyDelete(ClaimsPrincipal claimsUser, string credentialId)
    {
        var user = await _userManager.GetUserAsync(claimsUser) ?? throw new ApplicationException("no user");

        byte[] credIdBytes;
        try
        {
            credIdBytes = Base64Url.DecodeFromChars(credentialId);    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing key");
            throw new ApplicationException("Error parsing keyId");
        }

        var result = await _userManager.RemovePasskeyAsync(user, credIdBytes);
        if (!result.Succeeded)
        {
            var errors = string.Join(',', result.Errors);
            throw new ApplicationException($"Could not remove passkey: {errors}");
        }
    }
}