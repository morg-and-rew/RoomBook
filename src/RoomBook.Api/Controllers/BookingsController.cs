using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Common;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>US4/US11: создание брони с проверкой конфликтов.</summary>
    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(BookingCreateDto dto)
    {
        var booking = await _bookingService.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetMy), new { }, booking);
    }

    /// <summary>US5: список собственных заявок пользователя.</summary>
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetMy()
    {
        var bookings = await _bookingService.GetMyBookingsAsync(User.GetUserId());
        return Ok(bookings);
    }

    /// <summary>US6: отмена собственной заявки.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Cancel(Guid id)
    {
        var booking = await _bookingService.CancelAsync(User.GetUserId(), id);
        return Ok(booking);
    }

    /// <summary>US9: подтверждение заявки администратором.</summary>
    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingDto>> Approve(Guid id)
    {
        var booking = await _bookingService.ApproveAsync(id);
        return Ok(booking);
    }

    /// <summary>US9: отклонение заявки администратором с указанием причины.</summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BookingDto>> Reject(Guid id, BookingRejectDto dto)
    {
        var booking = await _bookingService.RejectAsync(id, dto.Reason);
        return Ok(booking);
    }
}
