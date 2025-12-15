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

    [HttpPost("PasskeyCreationOptions")]
    public async Task<ActionResult<string>> PasskeyCreationOptions()
    {
        var options = await _webAuthnService.PasskeyCreationOptions(User);
        if (options is null)
            return NotFound();

        return Ok(options);
    }

    [HttpPost("PasskeyCreate")]
    public async Task<ActionResult> PasskeyCreate([FromBody] string options)
    {
        try
        {
            await _webAuthnService.PasskeyCreate(User, options);
        }
        catch(ApplicationException ae)
        {
            ModelState.AddModelError("invalid", ae.Message);
            return BadRequest(ModelState);
        }
        return Ok();
    }
}