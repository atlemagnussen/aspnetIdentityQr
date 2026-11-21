using System.Text.RegularExpressions;
using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace UserAdmin.Api.Roles;

public class RoleDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int UserCount { get; set; }
    /// <summary>System roles (UserRoles enum, e.g. Admin) can't be renamed or deleted</summary>
    public required bool IsSystem { get; set; }
}

public class RoleNameDto
{
    public required string Name { get; set; }
}

/// <summary>
/// Free-form roles. Role names are emitted as "role" claims in tokens by the ID server's profile service,
/// so renaming a role affects other apps that check for the old name.
/// </summary>
public partial class RolesService(ApplicationDbContext db, RoleManager<IdentityRole> roles)
{
    private static readonly HashSet<string> SystemRoles =
        new(Enum.GetNames<UserRoles>(), StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._:-]{0,63}$")]
    private static partial Regex ValidName();

    public async Task<List<RoleDto>> List() =>
        (await db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Name, UserCount = db.UserRoles.Count(ur => ur.RoleId == r.Id) })
            .ToListAsync())
        .Select(r => new RoleDto { Id = r.Id, Name = r.Name!, UserCount = r.UserCount, IsSystem = SystemRoles.Contains(r.Name!) })
        .ToList();

    public async Task<RoleDto> Create(string name)
    {
        name = Validate(name);
        var role = new IdentityRole(name);
        Ensure(await roles.CreateAsync(role)); // RoleValidator rejects duplicates
        return await Get(role.Id);
    }

    public async Task<RoleDto> Rename(string id, string name)
    {
        name = Validate(name);
        var role = await Find(id);
        if (SystemRoles.Contains(role.Name!))
            throw new ApplicationException($"'{role.Name}' is a system role and can't be renamed.");
        Ensure(await roles.SetRoleNameAsync(role, name));
        Ensure(await roles.UpdateAsync(role));
        return await Get(id);
    }

    public async Task Delete(string id)
    {
        var role = await Find(id);
        if (SystemRoles.Contains(role.Name!))
            throw new ApplicationException($"'{role.Name}' is a system role and can't be deleted.");
        Ensure(await roles.DeleteAsync(role)); // user assignments cascade
    }

    private async Task<RoleDto> Get(string id) =>
        (await List()).Single(r => r.Id == id);

    private async Task<IdentityRole> Find(string id) =>
        await roles.FindByIdAsync(id) ?? throw new ApplicationException($"Role {id} not found.");

    private static string Validate(string? name)
    {
        name = name?.Trim() ?? "";
        if (!ValidName().IsMatch(name))
            throw new ApplicationException("Role names are 1-64 characters: letters, digits and . _ : - (no spaces).");
        return name;
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ApplicationException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
