using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Common;
using RoomBook.Api.Data;
using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Возвращает активные помещения. Если передана дата, для каждого
    /// помещения дополнительно возвращаются занятые интервалы на сутки,
    /// начиная с этого момента (отдельным запросом по броням) — по ним UI
    /// показывает занятость (ФТ1). "2026-10-01" — сутки по UTC,
    /// "2026-10-01T00:00:00+03:00" — сутки по московскому времени.
    /// </summary>
    public async Task<IReadOnlyList<RoomDto>> GetAllAsync(DateTime? date, int? capacity, string[]? equipment)
    {
        var query = _db.Rooms.Where(r => r.IsActive).AsQueryable();

        if (capacity is > 0)
        {
            query = query.Where(r => r.Capacity >= capacity);
        }

        if (equipment is { Length: > 0 })
        {
            foreach (var item in equipment)
            {
                var needed = item;
                query = query.Where(r => r.Equipment.Contains(needed));
            }
        }

        var rooms = await query.OrderBy(r => r.Name).ToListAsync();

        if (date is null)
        {
            return rooms.Select(r => ToDto(r)).ToList();
        }

        var dayStart = date.Value.AsUtc();
        var dayEnd = dayStart.AddDays(1);
        var roomIds = rooms.Select(r => r.Id).ToList();

        var bookings = await _db.Bookings
            .Where(b => roomIds.Contains(b.RoomId) &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                        b.StartTime < dayEnd && b.EndTime > dayStart)
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        var slotsByRoom = bookings.ToLookup(b => b.RoomId,
            b => new RoomBusySlotDto(b.StartTime, b.EndTime, b.Status.ToString()));

        return rooms.Select(r => ToDto(r, slotsByRoom[r.Id].ToList())).ToList();
    }

    public async Task<RoomDto> CreateAsync(RoomCreateDto dto)
    {
        var room = new Room
        {
            Name = dto.Name.Trim(),
            Capacity = dto.Capacity,
            Equipment = dto.Equipment ?? Array.Empty<string>()
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        return ToDto(room);
    }

    public async Task<RoomDto> UpdateAsync(Guid id, RoomUpdateDto dto)
    {
        var room = await _db.Rooms.FindAsync(id)
            ?? throw ApiException.NotFound("Помещение не найдено.");

        room.Name = dto.Name.Trim();
        room.Capacity = dto.Capacity;
        room.Equipment = dto.Equipment ?? Array.Empty<string>();
        room.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return ToDto(room);
    }

    /// <summary>Мягкое удаление — помещение скрывается из выдачи, но история броней сохраняется.</summary>
    public async Task DeleteAsync(Guid id)
    {
        var room = await _db.Rooms.FindAsync(id)
            ?? throw ApiException.NotFound("Помещение не найдено.");

        room.IsActive = false;
        await _db.SaveChangesAsync();
    }

    private static RoomDto ToDto(Room r, IReadOnlyList<RoomBusySlotDto>? busySlots = null) =>
        new(r.Id, r.Name, r.Capacity, r.Equipment, r.IsActive, busySlots);
}
