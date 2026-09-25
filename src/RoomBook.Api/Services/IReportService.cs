using RoomBook.Api.Dtos;

namespace RoomBook.Api.Services;

public interface IReportService
{
    Task<OccupancyReportDto> GetOccupancyReportAsync(DateTime dateFrom, DateTime dateTo);
}
