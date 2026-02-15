using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserAdmin.Api.Configuration.IdentityResources;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class IdentityResourcesController : ControllerBase
{
    private readonly IdentityResourcesService _service;

    public IdentityResourcesController(IdentityResourcesService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IEnumerable<IdentityResource>> List()
    {
        return _service.List();
    }

    [HttpGet("{id}")]
    public Task<IdentityResource> Get(string id)
    {
        return _service.Get(id);
    }
}