using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record RegisterDto(
    [property: Required, MaxLength(200)] string FullName,
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(6)] string Password
);

public record LoginDto(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password
);

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role
);
