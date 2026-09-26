using System.ComponentModel.DataAnnotations;

namespace RoomBook.Api.Dtos;

public record BookingCreateDto(
    [Range(1, long.MaxValue)] long RoomId,
    [Required] DateTime StartAt,
    [Required] DateTime EndAt,
    [MaxLength(500)] string? Purpose
);

public record BookingRejectDto(
    [MaxLength(500)] string? Reason
);

public record BookingDto(
    long Id,
    long RoomId,
    string RoomName,
    long UserId,
    string UserFullName,
    string Purpose,
    DateTime StartAt,
    DateTime EndAt,
    string Status,
    string? RejectReason,
    DateTime? DecidedAt,
    string? DecidedByName,
    DateTime CreatedAt
);
