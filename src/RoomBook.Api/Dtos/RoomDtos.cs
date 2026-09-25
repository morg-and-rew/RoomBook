using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record RoomDto(
    Guid Id,
    string Name,
    int Capacity,
    string[] Equipment,
    bool IsActive,
    IReadOnlyList<RoomBusySlotDto>? BusySlots = null
);

/// <summary>Занятый интервал помещения (заявка в статусе Pending или Approved).</summary>
public record RoomBusySlotDto(
    DateTime StartTime,
    DateTime EndTime,
    string Status
);

public record RoomCreateDto(
    [Required, MaxLength(200)] string Name,
    [Range(1, 1000)] int Capacity,
    string[]? Equipment
);

public record RoomUpdateDto(
    [Required, MaxLength(200)] string Name,
    [Range(1, 1000)] int Capacity,
    string[]? Equipment,
    bool IsActive
);
