using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User> SaveAsync(User user)
    {
        if (user.Id == 0)
        {
            _db.Users.Add(user);
        }
        await _db.SaveChangesAsync();
        return user;
    }

    public Task<User?> FindByIdAsync(long id) => _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> FindByEmailAsync(string email) => _db.Users.FirstOrDefaultAsync(u => u.Email == email);
}
