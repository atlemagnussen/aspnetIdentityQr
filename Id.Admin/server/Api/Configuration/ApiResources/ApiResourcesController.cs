using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Id.Admin.Api.Configuration.ApiResources;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class ApiResourcesController(ApiResourcesService service) : ControllerBase
{
    [HttpGet]
    public Task<List<ApiResourceDto>> List() => service.List();

    [HttpGet("{id}")]
    public Task<ApiResourceDto> Get(string id) => service.Get(id);
}
