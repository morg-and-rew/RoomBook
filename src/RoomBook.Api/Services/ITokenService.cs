using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
