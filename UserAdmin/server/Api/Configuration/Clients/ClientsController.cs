using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAdmin.Api.Configuration.Clients.Models;

namespace UserAdmin.Api.Configuration.Clients;

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class ClientsController(ClientsService service) : ControllerBase
{
    [HttpGet]
    public Task<List<ClientSummaryDto>> List() => service.List();

    [HttpGet("{id}")]
    public Task<ClientDto> Get(string id) => service.Get(id);

    [HttpPost]
    public Task<ClientDto> Create([FromBody] ClientDto dto) => service.Create(dto);

    [HttpPut("{id}")]
    public Task<ClientDto> Update(string id, [FromBody] ClientDto dto) => service.Update(id, dto);

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await service.Delete(id);
        return NoContent();
    }

    [HttpGet("{id}/secrets")]
    public Task<List<ClientSecretDto>> ListSecrets(string id) => service.ListSecrets(id);

    [HttpPost("{id}/secrets")]
    public Task<ClientSecretCreatedDto> CreateSecret(string id, [FromBody] ClientSecretCreateDto dto) =>
        service.CreateSecret(id, dto);

    [HttpDelete("{id}/secrets/{secretId:int}")]
    public async Task<IActionResult> DeleteSecret(string id, int secretId)
    {
        await service.DeleteSecret(id, secretId);
        return NoContent();
    }
}

[Authorize(Policies.RequiresAdmin)]
[ApiController]
[Route("api/[controller]")]
public class ScopesController(ClientsService service) : ControllerBase
{
    /// <summary>Identity resources + API scopes, for the client scope picker</summary>
    [HttpGet]
    public Task<List<ScopeDto>> List() => service.ListScopes();
}
