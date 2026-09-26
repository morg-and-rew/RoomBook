namespace RoomBook.Api.Entities;

/// <summary>Элемент справочника оборудования (проектор, доска, ...).</summary>
public class Equipment
{
    private Equipment() { } // для EF Core

    public Equipment(int id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public int Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public string GetDisplayName() => Name;
}
