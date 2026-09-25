using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>US1/US3: список помещений с фильтрами по вместимости и оборудованию. Доступно гостю.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetAll(
        [FromQuery] DateTime? date, [FromQuery] int? capacity, [FromQuery] string[]? equipment)
    {
        var rooms = await _roomService.GetAllAsync(date, capacity, equipment);
        return Ok(rooms);
    }

    /// <summary>US8: добавление помещения (только администратор).</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Create(RoomCreateDto dto)
    {
        var room = await _roomService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { }, room);
    }

    /// <summary>US8: редактирование помещения (только администратор).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RoomDto>> Update(Guid id, RoomUpdateDto dto)
    {
        var room = await _roomService.UpdateAsync(id, dto);
        return Ok(room);
    }

    /// <summary>US8: мягкое удаление помещения (только администратор).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _roomService.DeleteAsync(id);
        return NoContent();
    }
}
