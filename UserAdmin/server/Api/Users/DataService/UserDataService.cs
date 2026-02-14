using AspAuth.Lib.Data;
using Microsoft.EntityFrameworkCore;
using UserAdmin.Api.Users.Models;
using UserAdmin.Api.Users.Translators;

namespace UserAdmin.Api.Users.DataService;

public class UserDataService
{
    private readonly ApplicationDbContext _context;

    public UserDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDTO>> List()
    {
        var users = await _context.Users.Include(u => u.UserProfile).ToListAsync();
        var usersDto = ApplicationUserToUserDTO.Translate(users);
        return usersDto;
    }
}