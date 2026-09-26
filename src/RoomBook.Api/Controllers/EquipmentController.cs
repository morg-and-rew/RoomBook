using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly IRoomService _roomService;

    public EquipmentController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    /// <summary>Справочник оборудования: коды для фильтра и формы помещения.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> GetAll() =>
        Ok((await _roomService.ListEquipmentAsync()).Select(e => e.ToDto()).ToList());
}
