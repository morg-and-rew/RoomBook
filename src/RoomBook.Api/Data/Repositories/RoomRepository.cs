using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Common;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Data.Repositories;

public class RoomRepository : IRoomRepository
{
    private readonly AppDbContext _db;

    public RoomRepository(AppDbContext db)
    {
        _db = db;
    }

    private IQueryable<Room> Rooms => _db.Rooms.Include(r => r.EquipmentItems);

    public async Task<Room> SaveAsync(Room room)
    {
        if (room.Id == 0)
        {
            _db.Rooms.Add(room);
        }
        await _db.SaveChangesAsync();
        return room;
    }

    public Task<Room?> FindByIdAsync(long id) => Rooms.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IReadOnlyList<Room>> FindAvailableAsync(
        int? capacity, IReadOnlyCollection<string> equipmentCodes, DateTime? date)
    {
        var query = Rooms.Where(r => r.IsActive);

        if (capacity is > 0)
        {
            query = query.Where(r => r.Capacity >= capacity);
        }

        foreach (var code in equipmentCodes)
        {
            query = query.Where(r => r.EquipmentItems.Any(e => e.Code == code));
        }

        if (date is not null)
        {
            var dayStart = date.Value.AsUtc();
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(r => !_db.Bookings.Any(b =>
                b.RoomId == r.Id
                && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                && b.StartAt <= dayStart && b.EndAt >= dayEnd));
        }

        return await query.OrderBy(r => r.Building).ThenBy(r => r.Name).ToListAsync();
    }

    public async Task<IReadOnlyList<Room>> FindByBuildingAsync(string building) =>
        await Rooms.Where(r => r.Building == building).OrderBy(r => r.Name).ToListAsync();
}
