using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Common;
using RoomBook.Api.Data;
using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Отчёт по загруженности помещений за период (ФТ11).</summary>
    public async Task<OccupancyReportDto> GetOccupancyReportAsync(DateTime dateFrom, DateTime dateTo)
    {
        dateFrom = dateFrom.AsUtc();
        dateTo = dateTo.AsUtc();

        var bookings = await _db.Bookings
            .Include(b => b.Room)
            .Where(b => b.Status == BookingStatus.Approved &&
                        b.StartTime >= dateFrom && b.StartTime <= dateTo)
            .ToListAsync();

        var rooms = bookings
            .GroupBy(b => new { b.RoomId, RoomName = b.Room!.Name })
            .Select(g => new RoomOccupancyDto(
                g.Key.RoomId,
                g.Key.RoomName,
                g.Count(),
                Math.Round(g.Sum(b => (b.EndTime - b.StartTime).TotalHours), 1)))
            .OrderByDescending(r => r.TotalHoursBooked)
            .ToList();

        return new OccupancyReportDto(dateFrom, dateTo, rooms);
    }
}
