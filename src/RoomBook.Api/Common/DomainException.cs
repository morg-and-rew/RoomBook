namespace RoomBook.Api.Common;

/// <summary>Нарушение бизнес-правила сущности; middleware отвечает на него кодом 400.</summary>
public class DomainException : ApiException
{
    public DomainException(string message) : base(message) { }
}
