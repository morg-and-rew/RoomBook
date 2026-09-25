using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record BookingCreateDto(
    [property: Required] Guid RoomId,
    [property: Required] DateTime StartTime,
    [property: Required] DateTime EndTime,
    [property: MaxLength(500)] string? Purpose
);

public record BookingRejectDto(
    [property: MaxLength(500)] string? Reason
);

public record BookingDto(
    Guid Id,
    Guid RoomId,
    string RoomName,
    Guid UserId,
    string UserFullName,
    DateTime StartTime,
    DateTime EndTime,
    string Purpose,
    string Status,
    string? RejectionReason,
    DateTime CreatedAt
);
