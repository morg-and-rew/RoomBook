using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Common;
using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IRoomService _roomService;
    private readonly IUserService _userService;

    public BookingsController(IBookingService bookingService, IRoomService roomService, IUserService userService)
    {
        _bookingService = bookingService;
        _roomService = roomService;
        _userService = userService;
    }

    /// <summary>US4/US11: создание заявки с проверкой пересечений.</summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(BookingCreateDto dto)
    {
        var user = await CurrentUserAsync();
        var room = await _roomService.GetRoomAsync(dto.RoomId);
        var booking = await _bookingService.CreateBookingAsync(user, room, dto.StartAt, dto.EndAt, dto.Purpose);
        return CreatedAtAction(nameof(GetMy), new { }, booking.ToDto());
    }

    /// <summary>US5: заявки текущего пользователя.</summary>
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetMy()
    {
        var bookings = await _bookingService.ListUserBookingsAsync(await CurrentUserAsync());
        return Ok(bookings.Select(b => b.ToDto()).ToList());
    }

    /// <summary>US9: все заявки для администратора, с фильтром по статусу.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetAll([FromQuery] BookingStatus? status)
    {
        var bookings = await _bookingService.ListAllBookingsAsync(status);
        return Ok(bookings.Select(b => b.ToDto()).ToList());
    }

    /// <summary>US6: отмена заявки — автором или администратором.</summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<BookingDto>> Cancel(long id)
    {
        var booking = await _bookingService.CancelBookingAsync(id, await CurrentUserAsync());
        return Ok(booking.ToDto());
    }

    /// <summary>US9: подтверждение заявки администратором.</summary>
    [HttpPut("{id:long}/confirm")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingDto>> Confirm(long id)
    {
        var booking = await _bookingService.ConfirmBookingAsync(id, await CurrentUserAsync());
        return Ok(booking.ToDto());
    }

    /// <summary>US9: отклонение заявки администратором с указанием причины.</summary>
    [HttpPut("{id:long}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingDto>> Reject(long id, BookingRejectDto dto)
    {
        var booking = await _bookingService.RejectBookingAsync(id, await CurrentUserAsync(), dto.Reason);
        return Ok(booking.ToDto());
    }

    private Task<Entities.User> CurrentUserAsync() => _userService.GetByIdAsync(User.GetUserId());
}
