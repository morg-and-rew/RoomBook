using RoomBook.Api.Entities;

namespace RoomBook.Api.Repositories;

public interface IRoomRepository
{
    Task<Room> SaveAsync(Room room);
    Task<Room?> FindByIdAsync(long id);

    /// <summary>
    /// Активные помещения с нужной вместимостью и оборудованием. Если передана дата,
    /// исключаются помещения, занятые на все сутки начиная с этого момента.
    /// </summary>
    Task<IReadOnlyList<Room>> FindAvailableAsync(int? capacity, IReadOnlyCollection<string> equipmentCodes, DateTime? date);

    Task<IReadOnlyList<Room>> FindByBuildingAsync(string building);
}
