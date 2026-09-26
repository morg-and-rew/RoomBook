using RoomBook.Api.Entities;

namespace RoomBook.Api.Repositories;

/// <summary>
/// Справочник оборудования. На диаграмме классов отдельного репозитория нет,
/// но он нужен, чтобы RoomService находил оборудование по кодам, а сайт — показывал список.
/// </summary>
public interface IEquipmentRepository
{
    Task<IReadOnlyList<Equipment>> FindAllAsync();
    Task<IReadOnlyList<Equipment>> FindByCodesAsync(IReadOnlyCollection<string> codes);
}
