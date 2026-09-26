namespace RoomBook.Api.Services;

/// <summary>Отчёт по загруженности помещений за период.</summary>
public record Report(DateTime From, DateTime To, IReadOnlyList<RoomUtilization> Rooms);

public record RoomUtilization(long RoomId, string RoomName, string Building, int TotalBookings, double TotalHours);

/// <summary>Файл для скачивания (CSV-выгрузка отчёта).</summary>
public record ReportFile(string FileName, string ContentType, byte[] Content);

public interface IReportService
{
    Task<Report> UtilizationReportAsync(DateTime from, DateTime to);
    ReportFile ExportCsv(Report report);
}
