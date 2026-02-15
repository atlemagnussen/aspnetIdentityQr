using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserAdmin.Api.Configuration.ApiResources;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class ApiResourcesController : ControllerBase
{
    private readonly ApiResourcesService _service;

    public ApiResourcesController(ApiResourcesService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IEnumerable<ApiResource>> List()
    {
        return _service.List();
    }

    [HttpGet("{id}")]
    public Task<ApiResource> Get(string id)
    {
        return _service.Get(id);
    }
}