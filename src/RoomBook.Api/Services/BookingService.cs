using System.Data;
using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Common;
using RoomBook.Api.Data;
using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public BookingService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    /// <summary>
    /// Создание заявки на бронирование (ФТ4) с проверкой пересечения
    /// по времени (ФТ5, US11). Проверка и вставка выполняются в одной
    /// транзакции с уровнем изоляции Serializable, чтобы исключить
    /// состояние гонки при одновременных заявках на одно и то же время
    /// (НФТ3) — вторая параллельная транзакция получит ошибку сериализации
    /// и будет отклонена как конфликтующая.
    /// </summary>
    public async Task<BookingDto> CreateAsync(Guid userId, BookingCreateDto dto)
    {
        var startTime = dto.StartTime.AsUtc();
        var endTime = dto.EndTime.AsUtc();

        if (endTime <= startTime)
        {
            throw new ApiException("Время окончания должно быть позже времени начала.");
        }

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId && r.IsActive)
            ?? throw ApiException.NotFound("Помещение не найдено или недоступно для бронирования.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var hasConflict = await _db.Bookings.AnyAsync(b =>
            b.RoomId == dto.RoomId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
            b.StartTime < endTime &&
            b.EndTime > startTime);

        if (hasConflict)
        {
            throw ApiException.Conflict("Помещение уже забронировано на пересекающееся время.");
        }

        var booking = new Booking
        {
            RoomId = dto.RoomId,
            UserId = userId,
            StartTime = startTime,
            EndTime = endTime,
            Purpose = dto.Purpose ?? string.Empty,
            Status = BookingStatus.Pending
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notifications.NotifyAsync(userId, $"Заявка на бронирование «{room.Name}» создана и ожидает подтверждения.");

        booking.Room = room;
        return await MapToDto(booking);
    }

    public async Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(Guid userId)
    {
        var bookings = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bookings.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<BookingDto>> GetAllAsync(BookingStatus? status)
    {
        var query = _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }

        var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        return bookings.Select(ToDto).ToList();
    }

    public async Task<BookingDto> CancelAsync(Guid userId, Guid bookingId)
    {
        var booking = await _db.Bookings.Include(b => b.Room).Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw ApiException.NotFound("Заявка не найдена.");

        if (booking.UserId != userId)
        {
            throw ApiException.Forbidden("Нельзя отменить чужую заявку.");
        }

        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Rejected)
        {
            throw new ApiException("Заявка уже отменена или отклонена.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToDto(booking);
    }

    public async Task<BookingDto> ApproveAsync(Guid bookingId)
    {
        var booking = await GetForAdminAction(bookingId);

        booking.Status = BookingStatus.Approved;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.NotifyAsync(booking.UserId,
            $"Ваша заявка на «{booking.Room!.Name}» подтверждена администратором.");

        return ToDto(booking);
    }

    public async Task<BookingDto> RejectAsync(Guid bookingId, string? reason)
    {
        var booking = await GetForAdminAction(bookingId);

        booking.Status = BookingStatus.Rejected;
        booking.RejectionReason = reason;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var reasonSuffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" Причина: {reason}.";
        await _notifications.NotifyAsync(booking.UserId,
            $"Ваша заявка на «{booking.Room!.Name}» отклонена.{reasonSuffix}");

        return ToDto(booking);
    }

    private async Task<Booking> GetForAdminAction(Guid bookingId)
    {
        var booking = await _db.Bookings.Include(b => b.Room).Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw ApiException.NotFound("Заявка не найдена.");

        if (booking.Status != BookingStatus.Pending)
        {
            throw new ApiException("Решение можно принять только по заявке в статусе «Ожидает подтверждения».");
        }

        return booking;
    }

    private async Task<BookingDto> MapToDto(Booking booking)
    {
        booking.User ??= await _db.Users.FindAsync(booking.UserId);
        return ToDto(booking);
    }

    private static BookingDto ToDto(Booking b) => new(
        b.Id, b.RoomId, b.Room?.Name ?? string.Empty,
        b.UserId, b.User?.FullName ?? string.Empty,
        b.StartTime, b.EndTime, b.Purpose,
        b.Status.ToString(), b.RejectionReason, b.CreatedAt);
}
