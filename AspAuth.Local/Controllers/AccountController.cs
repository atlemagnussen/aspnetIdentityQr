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

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<string>> PasskeyRequestOptions(string userName)
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
}