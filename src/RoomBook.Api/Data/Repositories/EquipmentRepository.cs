using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Data.Repositories;

public class EquipmentRepository : IEquipmentRepository
{
    private readonly AppDbContext _db;

    public EquipmentRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Equipment>> FindAllAsync() =>
        await _db.Equipment.OrderBy(e => e.Name).ToListAsync();

    public async Task<IReadOnlyList<Equipment>> FindByCodesAsync(IReadOnlyCollection<string> codes) =>
        await _db.Equipment.Where(e => codes.Contains(e.Code)).ToListAsync();
}
