using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAdmin.Api.Users.DataService;
using UserAdmin.Api.Users.Models;

namespace UserAdmin.Api.Users;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserDataService _service;

    public UsersController(UserDataService service)
    {
        _service = service;
    }
    [HttpGet]
    public async Task<List<UserDTO>> List()
    {
        var users = await _service.List();
        return users;
    }

    [HttpGet("{id}")]
    public Task<UserDTO> Get(string id)
    {
        return _service.Get(id);
    }

    [HttpGet("{id}/roles")]
    public async Task<IList<string>> GetUserRoles(string id)
    {
        var roles = await _service.GetRoles(id);
        return roles ?? [];
    }

    [HttpPut("{id}/roles")]
    public async Task<IActionResult> AddUserRoles(string id, UserRoles role)
    {
        await _service.AddRole(id, role);
        return Ok();
    }
}