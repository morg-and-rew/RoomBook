using System.Globalization;
using System.Text;
using RoomBook.Api.Common;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Services;

public class ReportService : IReportService
{
    private readonly IBookingRepository _bookingRepository;

    public ReportService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    /// <summary>Отчёт по загруженности помещений за период (ФТ11): подтверждённые брони.</summary>
    public async Task<Report> UtilizationReportAsync(DateTime from, DateTime to)
    {
        from = from.AsUtc();
        to = to.AsUtc();
        if (to < from)
        {
            throw new DomainException("Начало периода должно быть раньше его окончания.");
        }

        var bookings = await _bookingRepository.FindConfirmedStartingInAsync(from, to);
        var rooms = bookings
            .GroupBy(b => b.Room)
            .Select(g => new RoomUtilization(
                g.Key.Id,
                g.Key.Name,
                g.Key.Building,
                g.Count(),
                Math.Round(g.Sum(b => (b.EndAt - b.StartAt).TotalHours), 1)))
            .OrderByDescending(r => r.TotalHours)
            .ToList();

        return new Report(from, to, rooms);
    }

    /// <summary>CSV для Excel: разделитель «;», UTF-8 с BOM, чтобы кириллица открылась корректно.</summary>
    public ReportFile ExportCsv(Report report)
    {
        var ru = CultureInfo.GetCultureInfo("ru-RU");
        var csv = new StringBuilder();
        csv.AppendLine("Помещение;Корпус;Подтверждённых броней;Часов");
        foreach (var room in report.Rooms)
        {
            csv.AppendLine(string.Join(';',
                Escape(room.RoomName), Escape(room.Building),
                room.TotalBookings.ToString(ru), room.TotalHours.ToString("0.#", ru)));
        }

        var fileName = $"utilization_{report.From:yyyy-MM-dd}_{report.To:yyyy-MM-dd}.csv";
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return new ReportFile(fileName, "text/csv; charset=utf-8", content);
    }

    private static string Escape(string value) =>
        value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
