using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public interface IUserService
{
    Task<User> RegisterAsync(string email, string password, string fullName);
    Task<User> AuthenticateAsync(string email, string password);
    Task<User> UpdateProfileAsync(User user, string fullName);

    /// <summary>Пользователь из JWT текущего запроса (дополнение к диаграмме).</summary>
    Task<User> GetByIdAsync(long id);
}
