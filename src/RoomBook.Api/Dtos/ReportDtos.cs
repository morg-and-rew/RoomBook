namespace RoomBook.Api.Dtos;

public record RoomOccupancyDto(
    Guid RoomId,
    string RoomName,
    int TotalBookings,
    double TotalHoursBooked
);

public record OccupancyReportDto(
    DateTime DateFrom,
    DateTime DateTo,
    IReadOnlyList<RoomOccupancyDto> Rooms
);

public record NotificationDto(
    Guid Id,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
