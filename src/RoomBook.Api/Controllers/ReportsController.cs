using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>US10: отчёт по загруженности помещений за период.</summary>
    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyReportDto>> GetOccupancy(
        [FromQuery] DateTime dateFrom, [FromQuery] DateTime dateTo)
    {
        var report = await _reportService.GetOccupancyReportAsync(dateFrom, dateTo);
        return Ok(report);
    }
}
