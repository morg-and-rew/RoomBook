using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record RoomDto(
    Guid Id,
    string Name,
    int Capacity,
    string[] Equipment,
    bool IsActive
);

public record RoomCreateDto(
    [property: Required, MaxLength(200)] string Name,
    [property: Range(1, 1000)] int Capacity,
    string[]? Equipment
);

public record RoomUpdateDto(
    [property: Required, MaxLength(200)] string Name,
    [property: Range(1, 1000)] int Capacity,
    string[]? Equipment,
    bool IsActive
);
