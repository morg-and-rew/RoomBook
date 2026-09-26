using RoomBook.Api.Common;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly INotificationService _notifier;

    public BookingService(IBookingRepository bookingRepository, INotificationService notifier)
    {
        _bookingRepository = bookingRepository;
        _notifier = notifier;
    }

    /// <summary>
    /// Создание заявки (ФТ4) с проверкой пересечений (ФТ5, US11). Проверка здесь даёт
    /// понятный ответ в обычном случае; одновременные заявки на одно время отсекает
    /// ограничение-исключение в БД (НФТ3), см. BookingRepository.SaveAsync.
    /// </summary>
    public async Task<Booking> CreateBookingAsync(User user, Room room, DateTime startAt, DateTime endAt, string? purpose)
    {
        var booking = new Booking(user, room, startAt.AsUtc(), endAt.AsUtc(), purpose);

        var conflicts = await FindConflictsAsync(room, booking.StartAt, booking.EndAt);
        if (conflicts.Count > 0)
        {
            throw ApiException.Conflict("Помещение уже забронировано на пересекающееся время.");
        }

        await _bookingRepository.SaveAsync(booking);
        await _notifier.NotifyStatusChangeAsync(booking, NotificationType.Created);
        return booking;
    }

    public async Task<Booking> ConfirmBookingAsync(long bookingId, User admin)
    {
        var booking = await GetAsync(bookingId);

        // Страховка: подтверждаемая заявка не должна пересекаться с другой активной.
        var active = await _bookingRepository.FindActiveByRoomAsync(booking.Room);
        if (active.Any(other => other.Id != booking.Id && other.OverlapsWith(booking)))
        {
            throw ApiException.Conflict("На это время уже есть другая активная заявка.");
        }

        booking.Confirm(admin);
        await _bookingRepository.SaveAsync(booking);
        await _notifier.NotifyStatusChangeAsync(booking, NotificationType.Confirmed);
        return booking;
    }

    public async Task<Booking> RejectBookingAsync(long bookingId, User admin, string? reason)
    {
        var booking = await GetAsync(bookingId);
        booking.Reject(admin, reason);
        await _bookingRepository.SaveAsync(booking);
        await _notifier.NotifyStatusChangeAsync(booking, NotificationType.Rejected);
        return booking;
    }

    public async Task<Booking> CancelBookingAsync(long bookingId, User by)
    {
        var booking = await GetAsync(bookingId);
        booking.Cancel(by);
        await _bookingRepository.SaveAsync(booking);
        await _notifier.NotifyStatusChangeAsync(booking, NotificationType.Cancelled);
        return booking;
    }

    /// <summary>Активные заявки на помещение, пересекающиеся с периодом.</summary>
    public async Task<IReadOnlyList<Booking>> FindConflictsAsync(Room room, DateTime startAt, DateTime endAt)
    {
        var bookings = await _bookingRepository.FindByRoomAndPeriodAsync(room, startAt.AsUtc(), endAt.AsUtc());
        return bookings.Where(b => b.IsActive()).ToList();
    }

    public Task<IReadOnlyList<Booking>> ListUserBookingsAsync(User user) => _bookingRepository.FindByUserAsync(user);

    public Task<IReadOnlyList<Booking>> ListAllBookingsAsync(BookingStatus? status) => _bookingRepository.FindAllAsync(status);

    private async Task<Booking> GetAsync(long bookingId) =>
        await _bookingRepository.FindByIdAsync(bookingId) ?? throw ApiException.NotFound("Заявка не найдена.");
}
