using RoomBook.Api.Common;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Services;

public class RoomService : IRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    public RoomService(IRoomRepository roomRepository, IEquipmentRepository equipmentRepository)
    {
        _roomRepository = roomRepository;
        _equipmentRepository = equipmentRepository;
    }

    public async Task<IReadOnlyList<Room>> ListRoomsAsync(RoomFilter filters)
    {
        var rooms = await _roomRepository.FindAvailableAsync(filters.Capacity, filters.EquipmentCodes, filters.Date);
        return string.IsNullOrWhiteSpace(filters.Building)
            ? rooms
            : rooms.Where(r => r.Building == filters.Building.Trim()).ToList();
    }

    public async Task<Room> GetRoomAsync(long id) =>
        await _roomRepository.FindByIdAsync(id) ?? throw ApiException.NotFound("Помещение не найдено.");

    public async Task<Room> CreateRoomAsync(RoomData data)
    {
        await EnsureNameIsFreeAsync(data, roomId: null);
        var room = new Room(data.Name, data.Building, data.Floor, data.Capacity, data.Description);
        await ApplyEquipmentAsync(room, data.EquipmentCodes);
        return await _roomRepository.SaveAsync(room);
    }

    public async Task<Room> UpdateRoomAsync(long id, RoomData data)
    {
        var room = await GetRoomAsync(id);
        await EnsureNameIsFreeAsync(data, room.Id);
        room.Update(data.Name, data.Building, data.Floor, data.Capacity, data.Description);
        await ApplyEquipmentAsync(room, data.EquipmentCodes);
        return await _roomRepository.SaveAsync(room);
    }

    public async Task DeactivateRoomAsync(long id)
    {
        var room = await GetRoomAsync(id);
        room.Deactivate();
        await _roomRepository.SaveAsync(room);
    }

    public Task<IReadOnlyList<Equipment>> ListEquipmentAsync() => _equipmentRepository.FindAllAsync();

    /// <summary>В одном корпусе не может быть двух активных помещений с одинаковым названием.</summary>
    private async Task EnsureNameIsFreeAsync(RoomData data, long? roomId)
    {
        var sameBuilding = await _roomRepository.FindByBuildingAsync(data.Building.Trim());
        var duplicate = sameBuilding.Any(r => r.IsActive && r.Id != roomId
            && string.Equals(r.Name, data.Name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            throw ApiException.Conflict($"В корпусе «{data.Building.Trim()}» уже есть помещение «{data.Name.Trim()}».");
        }
    }

    /// <summary>Приводит оснащение помещения к списку кодов из справочника.</summary>
    private async Task ApplyEquipmentAsync(Room room, IReadOnlyCollection<string> codes)
    {
        var wanted = await _equipmentRepository.FindByCodesAsync(codes.Distinct().ToList());
        var unknown = codes.Except(wanted.Select(e => e.Code)).ToList();
        if (unknown.Count > 0)
        {
            throw new DomainException($"Неизвестное оборудование: {string.Join(", ", unknown)}.");
        }

        foreach (var eq in room.EquipmentItems.ToList())
        {
            if (wanted.All(w => w.Id != eq.Id)) room.RemoveEquipment(eq);
        }
        foreach (var eq in wanted)
        {
            room.AddEquipment(eq);
        }
    }
}
