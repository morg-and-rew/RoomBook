using RoomBook.Api.Entities;

namespace RoomBook.Api.Repositories;

public interface IUserRepository
{
    Task<User> SaveAsync(User user);
    Task<User?> FindByIdAsync(long id);
    Task<User?> FindByEmailAsync(string email);
}
