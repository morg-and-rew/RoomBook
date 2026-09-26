using RoomBook.Api.Entities;
using RoomBook.Api.Services;

namespace RoomBook.Api.Dtos;

/// <summary>Преобразование сущностей и моделей сервисов в DTO ответа API.</summary>
public static class DtoMapping
{
    public static UserDto ToDto(this User u) => new(u.Id, u.FullName, u.Email, u.Role.ToString());

    public static EquipmentDto ToDto(this Equipment e) => new(e.Id, e.Code, e.GetDisplayName());

    public static RoomDto ToDto(this Room r, IReadOnlyList<RoomBusySlotDto>? busySlots = null) => new(
        r.Id, r.Name, r.Building, r.Floor, r.Capacity, r.Description,
        r.EquipmentItems.OrderBy(e => e.Name).Select(e => e.ToDto()).ToList(),
        r.IsActive, busySlots);

    public static RoomBusySlotDto ToBusySlot(this Booking b) => new(b.StartAt, b.EndAt, b.Status.ToString());

    public static BookingDto ToDto(this Booking b) => new(
        b.Id, b.RoomId, b.Room.Name, b.UserId, b.User.FullName, b.Purpose,
        b.StartAt, b.EndAt, b.Status.ToString(), b.RejectReason,
        b.DecidedAt, b.DecidedBy?.FullName, b.CreatedAt);

    public static NotificationDto ToDto(this Notification n) => new(
        n.Id, n.Type?.ToString(), n.Message, n.BookingId, n.SentAt, n.ReadAt, n.ReadAt is not null);

    public static UtilizationReportDto ToDto(this Report report) => new(
        report.From, report.To,
        report.Rooms.Select(r => new RoomUtilizationDto(r.RoomId, r.RoomName, r.Building, r.TotalBookings, r.TotalHours)).ToList());
}
