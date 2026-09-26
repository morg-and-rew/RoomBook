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

    /// <summary>US10: загруженность помещений за период (подтверждённые брони).</summary>
    [HttpGet("utilization")]
    public async Task<ActionResult<UtilizationReportDto>> Utilization([FromQuery] DateTime from, [FromQuery] DateTime to) =>
        Ok((await _reportService.UtilizationReportAsync(from, to)).ToDto());

    /// <summary>US10: тот же отчёт в CSV для Excel.</summary>
    [HttpGet("utilization/csv")]
    public async Task<IActionResult> UtilizationCsv([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var file = _reportService.ExportCsv(await _reportService.UtilizationReportAsync(from, to));
        return File(file.Content, file.ContentType, file.FileName);
    }
}
