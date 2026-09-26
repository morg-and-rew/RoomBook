using Microsoft.EntityFrameworkCore;
using Npgsql;
using RoomBook.Api.Common;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Data.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<Booking> Bookings => _db.Bookings
        .Include(b => b.Room)
        .Include(b => b.User)
        .Include(b => b.DecidedBy);

    /// <summary>
    /// Пересечения активных заявок запрещает ограничение-исключение в БД
    /// (см. миграцию): если две заявки на одно время сохраняются одновременно,
    /// вторая получит отказ от PostgreSQL и превратится здесь в 409 Conflict.
    /// </summary>
    public async Task<Booking> SaveAsync(Booking booking)
    {
        if (booking.Id == 0)
        {
            _db.Bookings.Add(booking);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            _db.Entry(booking).State = EntityState.Detached;
            throw ApiException.Conflict("Помещение уже забронировано на пересекающееся время.");
        }
        return booking;
    }

    public Task<Booking?> FindByIdAsync(long id) => Bookings.FirstOrDefaultAsync(b => b.Id == id);

    public async Task<IReadOnlyList<Booking>> FindByRoomAndPeriodAsync(Room room, DateTime from, DateTime to) =>
        await Bookings
            .Where(b => b.RoomId == room.Id && b.StartAt < to && b.EndAt > from)
            .OrderBy(b => b.StartAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Booking>> FindActiveByRoomAsync(Room room) =>
        await Bookings
            .Where(b => b.RoomId == room.Id
                        && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed))
            .OrderBy(b => b.StartAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Booking>> FindByUserAsync(User user) =>
        await Bookings.Where(b => b.UserId == user.Id).OrderByDescending(b => b.CreatedAt).ToListAsync();

    public async Task<IReadOnlyList<Booking>> FindAllAsync(BookingStatus? status)
    {
        var query = Bookings;
        if (status is not null)
        {
            query = query.Where(b => b.Status == status);
        }
        return await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
    }

    public async Task<IReadOnlyList<Booking>> FindConfirmedStartingInAsync(DateTime from, DateTime to) =>
        await Bookings
            .Where(b => b.Status == BookingStatus.Confirmed && b.StartAt >= from && b.StartAt <= to)
            .ToListAsync();
}
