using AspAuth.Lib.Models;
using AspAuth.Lib.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspAuth.Local.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly WebAuthnService _webAuthnService;
    public AccountController(WebAuthnService webAuthnService)
    {
        _webAuthnService = webAuthnService;
    }

    /// <summary>
    /// Create options before create
    /// </summary>
    [HttpPost("PasskeyCreationOptions")]
    public async Task<ActionResult<string>> PasskeyCreationOptions()
    {
        var options = await _webAuthnService.PasskeyCreationOptions(User);
        if (options is null)
            return NotFound();

        return Ok(options);
    }

    /// <summary>
    /// Request options before login
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    [HttpPost("PasskeyRequestOptions")]
    [AllowAnonymous]
    public async Task<ActionResult<string>> PasskeyRequestOptions([FromQuery]string? userName)
    {
        try
        {
            var requestOptions = await _webAuthnService.GetPasskeyRequestOptions(userName);
            return Ok(requestOptions);
        }
        catch(ApplicationException ae)
        {
            ModelState.AddModelError("invalid", ae.Message);
            return BadRequest(ModelState);
        }
    }

    /// <summary>
    /// Actually create
    /// </summary>
    /// <param name="credentialJson">from navigator.credentials.create</param>
    [HttpPost("PasskeyCreate")]
    public async Task<ActionResult> PasskeyCreate([FromBody] string credentialJson)
    {
        try
        {
            await _webAuthnService.PasskeyCreate(User, credentialJson);
        }
        catch(ApplicationException ae)
        {
            ModelState.AddModelError("invalid", ae.Message);
            return BadRequest(ModelState);
        }
        return Ok();
    }

    /// <summary>
    /// Update name
    /// </summary>
    /// <param name="credentialJson">from navigator.credentials.create</param>
    [HttpPost("PasskeyUpdate")]
    public async Task<ActionResult> PasskeyUpdate([FromBody] PassKeyViewModel passkey)
    {
        try
        {
            await _webAuthnService.PasskeyRename(User, passkey.CredentialId, passkey.Name);
        }
        catch(ApplicationException ae)
        {
            ModelState.AddModelError("invalid", ae.Message);
            return BadRequest(ModelState);
        }
        return Ok();
    }

    /// <summary>
    /// Update name
    /// </summary>
    /// <param name="credentialJson">from navigator.credentials.create</param>
    [HttpPost("PasskeyDelete")]
    public async Task<ActionResult> PasskeyDelete([FromBody] PassKeyViewModel passkey)
    {
        try
        {
            await _webAuthnService.PasskeyDelete(User, passkey.CredentialId);
        }
        catch(ApplicationException ae)
        {
            ModelState.AddModelError("invalid", ae.Message);
            return BadRequest(ModelState);
        }
        return Ok();
    }
}