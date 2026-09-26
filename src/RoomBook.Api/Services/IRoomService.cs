using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

/// <summary>Фильтры списка помещений (ФТ3).</summary>
public record RoomFilter(int? Capacity, IReadOnlyCollection<string> EquipmentCodes, string? Building, DateTime? Date);

/// <summary>Данные для создания и изменения помещения.</summary>
public record RoomData(string Name, string Building, int Floor, int Capacity, string? Description,
    IReadOnlyCollection<string> EquipmentCodes);

public interface IRoomService
{
    Task<IReadOnlyList<Room>> ListRoomsAsync(RoomFilter filters);
    Task<Room> GetRoomAsync(long id);
    Task<Room> CreateRoomAsync(RoomData data);
    Task<Room> UpdateRoomAsync(long id, RoomData data);
    Task DeactivateRoomAsync(long id);

    /// <summary>Справочник оборудования (дополнение к диаграмме, нужен формам сайта).</summary>
    Task<IReadOnlyList<Equipment>> ListEquipmentAsync();
}
