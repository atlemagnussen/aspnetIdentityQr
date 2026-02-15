using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAdmin.Api.Configuration.Clients;

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
}