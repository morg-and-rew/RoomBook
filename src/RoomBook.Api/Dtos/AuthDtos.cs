using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record RegisterDto(
    [Required, MaxLength(200)] string FullName,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(6)] string Password
);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record UpdateProfileDto(
    [Required, MaxLength(200)] string FullName
);

public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    long Id,
    string FullName,
    string Email,
    string Role
);
