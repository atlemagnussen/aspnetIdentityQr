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
}