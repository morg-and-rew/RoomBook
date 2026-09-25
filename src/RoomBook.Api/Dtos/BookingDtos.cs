using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record BookingCreateDto(
    [Required] Guid RoomId,
    [Required] DateTime StartTime,
    [Required] DateTime EndTime,
    [MaxLength(500)] string? Purpose
);

public record BookingRejectDto(
    [MaxLength(500)] string? Reason
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
