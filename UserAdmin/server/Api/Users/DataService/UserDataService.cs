using AspAuth.Lib.Data;
using AspAuth.Lib.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserAdmin.Api.Users.Models;
using UserAdmin.Api.Users.Translators;

namespace UserAdmin.Api.Users.DataService;

public class UserDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _manager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserDataService(ApplicationDbContext context,
        UserManager<ApplicationUser> manager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _manager = manager;
        _roleManager = roleManager;
    }

    public async Task<List<UserDTO>> List()
    {
        var users = await _context.Users.Include(u => u.UserProfile).ToListAsync();
        var usersDto = ApplicationUserToUserDTO.Translate(users);
        return usersDto;
    }
    public async Task<UserDTO> Get(string userId)
    {
        var user = await _context.Users.Include(u => u.UserProfile).FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new ApplicationException("User does not exist");
        return ApplicationUserToUserDTO.Translate(user);
    }

    public async Task<IList<string>?> GetRoles(string userId)
    {
        var user = await _manager.FindByIdAsync(userId) ?? throw new ApplicationException("User does not exist");
        var roles = await _manager.GetRolesAsync(user);
        return roles;
    }

    public async Task AddRole(string userId, UserRoles role)
    {
        var user = await _manager.FindByIdAsync(userId) ?? throw new ApplicationException($"User {userId} not found");
        await EnsureRoleExist(role);

        await _manager.AddToRoleAsync(user, role.ToString());
    }

    private async Task<IdentityRole> EnsureRoleExist(UserRoles role)
    {
        IdentityRole? roleEntity = await _roleManager.FindByNameAsync(role.ToString());
        roleEntity ??= await CreateRole(role);
        return roleEntity;
    }

    private async Task<IdentityRole> CreateRole(UserRoles role)
    {
        var newRole = new IdentityRole
        {
            Name = role.ToString()
        };
        await _roleManager.CreateAsync(newRole);
        return newRole;
    }
}