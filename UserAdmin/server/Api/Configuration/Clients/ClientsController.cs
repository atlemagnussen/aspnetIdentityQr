using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAdmin.Api.Configuration.Clients.Model;

namespace UserAdmin.Api.Configuration.Clients;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly ClientsService _service;

    public ClientsController(ClientsService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<IEnumerable<Client>> List()
    {
        return _service.List();
    }

    [HttpGet("{id}")]
    public Task<Client> Get(string id)
    {
        return _service.Get(id);
    }

    [HttpPut("{id}/redirecturls")]
    public async Task<Client> AddRedirectUrl([FromRoute] string id, [FromBody] ClientUrlPatch patch)
    {
        var client = await _service.AddRedirectUri(id, patch.Url);
        return client;   
    }

    [HttpPut("{id}/corsorigins")]
    public async Task<Client> AddCorsOrigin([FromRoute] string id, [FromBody] ClientUrlPatch patch)
    {
        var client = await _service.AddCorsOrigin(id, patch.Url);
        return client;   
    }
}