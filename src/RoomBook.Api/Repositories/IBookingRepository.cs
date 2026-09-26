using RoomBook.Api.Entities;

namespace RoomBook.Api.Repositories;

public interface IBookingRepository
{
    Task<Booking> SaveAsync(Booking booking);
    Task<Booking?> FindByIdAsync(long id);

    /// <summary>Заявки на помещение, пересекающиеся с периодом [from, to), в любом статусе.</summary>
    Task<IReadOnlyList<Booking>> FindByRoomAndPeriodAsync(Room room, DateTime from, DateTime to);

    Task<IReadOnlyList<Booking>> FindActiveByRoomAsync(Room room);
    Task<IReadOnlyList<Booking>> FindByUserAsync(User user);

    // Дополнения к диаграмме классов: список заявок для администратора и выборка для отчёта.
    Task<IReadOnlyList<Booking>> FindAllAsync(BookingStatus? status);
    Task<IReadOnlyList<Booking>> FindConfirmedStartingInAsync(DateTime from, DateTime to);
}
