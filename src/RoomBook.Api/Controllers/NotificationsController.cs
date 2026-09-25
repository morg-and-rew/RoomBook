using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Common;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>US7: список уведомлений текущего пользователя.</summary>
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMy()
    {
        var items = await _notificationService.GetMyAsync(User.GetUserId());
        return Ok(items);
    }
}
