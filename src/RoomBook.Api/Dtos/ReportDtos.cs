namespace RoomBook.Api.Dtos;

public record RoomUtilizationDto(
    long RoomId,
    string RoomName,
    string Building,
    int TotalBookings,
    double TotalHours
);

public record UtilizationReportDto(
    DateTime From,
    DateTime To,
    IReadOnlyList<RoomUtilizationDto> Rooms
);

public record NotificationDto(
    long Id,
    string? Type,
    string Message,
    long? BookingId,
    DateTime SentAt,
    DateTime? ReadAt,
    bool IsRead
);
