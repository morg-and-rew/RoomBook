using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record EquipmentDto(
    int Id,
    string Code,
    string Name
);

public record RoomDto(
    long Id,
    string Name,
    string Building,
    int Floor,
    int Capacity,
    string? Description,
    IReadOnlyList<EquipmentDto> Equipment,
    bool IsActive,
    IReadOnlyList<RoomBusySlotDto>? BusySlots = null
);

/// <summary>Занятый интервал помещения: заявка в статусе Pending или Confirmed.</summary>
public record RoomBusySlotDto(
    DateTime StartAt,
    DateTime EndAt,
    string Status
);

/// <summary>Данные помещения для создания и изменения (RoomData на диаграмме).</summary>
public record RoomDataDto(
    [Required, MaxLength(200)] string Name,
    [Required, MaxLength(100)] string Building,
    [Range(-5, 200)] int Floor,
    [Range(1, 1000)] int Capacity,
    [MaxLength(1000)] string? Description,
    string[]? EquipmentCodes
);
