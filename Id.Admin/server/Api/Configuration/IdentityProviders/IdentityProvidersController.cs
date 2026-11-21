using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Id.Admin.Api.Configuration.IdentityProviders;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class IdentityProvidersController(IdentityProvidersService service) : ControllerBase
{
    [HttpGet]
    public Task<List<IdentityProviderDto>> List() => service.List();
}
