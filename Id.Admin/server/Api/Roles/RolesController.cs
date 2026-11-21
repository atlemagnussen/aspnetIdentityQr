using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Id.Admin.Api.Roles;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class RolesController(RolesService service) : ControllerBase
{
    [HttpGet]
    public Task<List<RoleDto>> List() => service.List();

    [HttpPost]
    public Task<RoleDto> Create([FromBody] RoleNameDto dto) => service.Create(dto.Name);

    [HttpPut("{id}")]
    public Task<RoleDto> Rename(string id, [FromBody] RoleNameDto dto) => service.Rename(id, dto.Name);

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await service.Delete(id);
        return NoContent();
    }
}
