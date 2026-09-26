using RoomBook.Api.Common;

namespace RoomBook.Api.Entities;

public class Room
{
    private Room() { } // для EF Core

    public Room(string name, string building, int floor, int capacity, string? description)
    {
        Update(name, building, floor, capacity, description);
    }

    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Building { get; private set; } = string.Empty;
    public int Floor { get; private set; }
    public int Capacity { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Оснащение помещения (таблица room_equipment).</summary>
    public ICollection<Equipment> EquipmentItems { get; private set; } = new List<Equipment>();

    public void Update(string name, string building, int floor, int capacity, string? description)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(building))
        {
            throw new DomainException("Укажите название помещения и корпус.");
        }
        if (capacity < 1)
        {
            throw new DomainException("Вместимость должна быть не меньше 1.");
        }

        Name = name.Trim();
        Building = building.Trim();
        Floor = floor;
        Capacity = capacity;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void AddEquipment(Equipment eq)
    {
        if (EquipmentItems.All(e => e.Id != eq.Id))
        {
            EquipmentItems.Add(eq);
        }
    }

    public void RemoveEquipment(Equipment eq)
    {
        var existing = EquipmentItems.FirstOrDefault(e => e.Id == eq.Id);
        if (existing is not null)
        {
            EquipmentItems.Remove(existing);
        }
    }

    /// <summary>Мягкое удаление: помещение скрывается, история броней сохраняется.</summary>
    public void Deactivate() => IsActive = false;

    public bool MatchesFilter(int? capacity, IReadOnlyCollection<string> equipmentCodes) =>
        IsActive
        && (capacity is null or <= 0 || Capacity >= capacity)
        && equipmentCodes.All(code => EquipmentItems.Any(e => e.Code == code));
}
