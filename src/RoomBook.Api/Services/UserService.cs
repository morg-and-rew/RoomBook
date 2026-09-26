using System.Net;
using RoomBook.Api.Common;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> RegisterAsync(string email, string password, string fullName)
    {
        var normalized = NormalizeEmail(email);
        if (await _userRepository.FindByEmailAsync(normalized) is not null)
        {
            throw new ApiException("Пользователь с таким email уже зарегистрирован.", HttpStatusCode.Conflict);
        }

        return await _userRepository.SaveAsync(new User(normalized, password, fullName));
    }

    public async Task<User> AuthenticateAsync(string email, string password)
    {
        var user = await _userRepository.FindByEmailAsync(NormalizeEmail(email));
        if (user is null || !user.VerifyPassword(password))
        {
            throw ApiException.Unauthorized("Неверный email или пароль.");
        }
        return user;
    }

    public async Task<User> UpdateProfileAsync(User user, string fullName)
    {
        user.ChangeFullName(fullName);
        return await _userRepository.SaveAsync(user);
    }

    public async Task<User> GetByIdAsync(long id) =>
        await _userRepository.FindByIdAsync(id)
        ?? throw ApiException.Unauthorized("Пользователь не найден. Войдите заново.");

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
