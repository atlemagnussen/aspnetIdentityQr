using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserAdmin.Api.Configuration.IdentityProviders;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class IdentityProvidersController : ControllerBase
{
    private readonly IdentityProvidersService _service;

    public IdentityProvidersController(IdentityProvidersService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IEnumerable<IdentityProvider>> List()
    {
        return _service.List();
    }
}