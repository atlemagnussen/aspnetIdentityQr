using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Id.Admin.Api.Configuration.IdentityResources;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class IdentityResourcesController(IdentityResourcesService service) : ControllerBase
{
    [HttpGet]
    public Task<List<IdentityResourceDto>> List() => service.List();

    [HttpGet("{id}")]
    public Task<IdentityResourceDto> Get(string id) => service.Get(id);
}
