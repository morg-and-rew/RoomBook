using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Common;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IBookingService _bookingService;

    public RoomsController(IRoomService roomService, IBookingService bookingService)
    {
        _roomService = roomService;
        _bookingService = bookingService;
    }

    /// <summary>
    /// US1/US3: помещения с фильтрами (вместимость, коды оборудования, корпус). Доступно гостю.
    /// С параметром date для каждого помещения возвращаются занятые интервалы на сутки
    /// с этого момента ("2026-10-01" — сутки по UTC, "2026-10-01T00:00:00+03:00" — по Москве).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(
        [FromQuery] DateTime? date, [FromQuery] int? capacity, [FromQuery] string[]? equipment, [FromQuery] string? building)
    {
        var rooms = await _roomService.ListRoomsAsync(
            new RoomFilter(capacity, equipment ?? Array.Empty<string>(), building, date));

        if (date is null)
        {
            return Ok(rooms.Select(r => r.ToDto()).ToList());
        }

        var dayStart = date.Value.AsUtc();
        var result = new List<RoomDto>();
        foreach (var room in rooms)
        {
            var busy = await _bookingService.FindConflictsAsync(room, dayStart, dayStart.AddDays(1));
            result.Add(room.ToDto(busy.Select(b => b.ToBusySlot()).ToList()));
        }
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<ActionResult<RoomDto>> Get(long id) => Ok((await _roomService.GetRoomAsync(id)).ToDto());

    /// <summary>US8: добавление помещения (только администратор).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Create(RoomDataDto dto)
    {
        var room = await _roomService.CreateRoomAsync(ToData(dto));
        return CreatedAtAction(nameof(Get), new { id = room.Id }, room.ToDto());
    }

    /// <summary>US8: редактирование помещения (только администратор).</summary>
    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Update(long id, RoomDataDto dto) =>
        Ok((await _roomService.UpdateRoomAsync(id, ToData(dto))).ToDto());

    /// <summary>US8: деактивация (мягкое удаление) помещения (только администратор).</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(long id)
    {
        await _roomService.DeactivateRoomAsync(id);
        return NoContent();
    }

    private static RoomData ToData(RoomDataDto dto) => new(
        dto.Name, dto.Building, dto.Floor, dto.Capacity, dto.Description, dto.EquipmentCodes ?? Array.Empty<string>());
}
