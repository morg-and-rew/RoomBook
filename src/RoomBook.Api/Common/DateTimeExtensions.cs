namespace RoomBook.Api.Common;

public static class DateTimeExtensions
{
    /// <summary>
    /// Приводит дату к UTC: Npgsql записывает в колонки timestamp with time zone
    /// только DateTime с Kind=Utc. Время без часового пояса (например,
    /// "2026-10-01" в query-строке) считается заданным в UTC.
    /// </summary>
    public static DateTime AsUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
