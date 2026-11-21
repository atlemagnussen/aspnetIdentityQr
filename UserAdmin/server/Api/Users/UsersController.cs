using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAdmin.Api.Users.Models;
using UserAdmin.Auth;

namespace UserAdmin.Api.Users;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class UsersController(UsersService service) : ControllerBase
{
    [HttpGet]
    public Task<List<UserListItemDto>> List() => service.List();

    [HttpGet("{id}")]
    public Task<UserDetailsDto> Get(string id) => service.Get(id);

    [HttpPost]
    public Task<UserCreatedDto> Create([FromBody] UserCreateDto dto) => service.Create(dto);

    [HttpPut("{id}")]
    public Task<UserDetailsDto> Update(string id, [FromBody] UserUpdateDto dto) => service.Update(id, dto);

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await service.Delete(id, User.GetUserId());
        return NoContent();
    }

    [HttpPut("{id}/roles/{role}")]
    public Task<UserDetailsDto> AddRole(string id, string role) => service.AddRole(id, role);

    [HttpDelete("{id}/roles/{role}")]
    public Task<UserDetailsDto> RemoveRole(string id, string role) => service.RemoveRole(id, role, User.GetUserId());

    [HttpPost("{id}/claims")]
    public Task<UserDetailsDto> AddClaim(string id, [FromBody] ClaimDto claim) => service.AddClaim(id, claim);

    [HttpDelete("{id}/claims")]
    public Task<UserDetailsDto> RemoveClaim(string id, [FromQuery] string type, [FromQuery] string value) =>
        service.RemoveClaim(id, type, value);

    [HttpPost("{id}/lock")]
    public Task<UserDetailsDto> Lock(string id, [FromBody] LockUserDto dto) => service.Lock(id, dto.Until, User.GetUserId());

    [HttpPost("{id}/unlock")]
    public Task<UserDetailsDto> Unlock(string id) => service.Unlock(id);

    [HttpPost("{id}/reset-2fa")]
    public Task<UserDetailsDto> ResetTwoFactor(string id) => service.ResetTwoFactor(id);

    [HttpDelete("{id}/logins")]
    public Task<UserDetailsDto> RemoveLogin(string id, [FromQuery] string provider, [FromQuery] string key) =>
        service.RemoveLogin(id, provider, key);

    [HttpDelete("{id}/passkeys/{credentialId}")]
    public Task<UserDetailsDto> RemovePasskey(string id, string credentialId) => service.RemovePasskey(id, credentialId);

    [HttpPost("{id}/sign-out-everywhere")]
    public Task<UserDetailsDto> SignOutEverywhere(string id) => service.SignOutEverywhere(id);

    [HttpPost("{id}/password-link")]
    public Task<PasswordLinkDto> CreatePasswordLink(string id) => service.CreatePasswordLink(id);
}
