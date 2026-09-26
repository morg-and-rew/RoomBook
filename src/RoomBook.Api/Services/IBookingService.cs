using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public interface IBookingService
{
    Task<Booking> CreateBookingAsync(User user, Room room, DateTime startAt, DateTime endAt, string? purpose);
    Task<Booking> ConfirmBookingAsync(long bookingId, User admin);
    Task<Booking> RejectBookingAsync(long bookingId, User admin, string? reason);
    Task<Booking> CancelBookingAsync(long bookingId, User by);
    Task<IReadOnlyList<Booking>> FindConflictsAsync(Room room, DateTime startAt, DateTime endAt);
    Task<IReadOnlyList<Booking>> ListUserBookingsAsync(User user);

    /// <summary>Все заявки для администратора (дополнение к диаграмме).</summary>
    Task<IReadOnlyList<Booking>> ListAllBookingsAsync(BookingStatus? status);
}
