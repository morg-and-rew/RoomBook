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
    /// помещения дополнительно вычисляется признак занятости на эту дату
    /// (через отдельный запрос статусов броней) — используется на UI
    /// для отображения статуса "свободно"/"занято" (ФТ1).
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
        return rooms.Select(ToDto).ToList();
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

    private static RoomDto ToDto(Room r) => new(r.Id, r.Name, r.Capacity, r.Equipment, r.IsActive);
}
