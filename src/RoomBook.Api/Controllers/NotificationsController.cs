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
    private readonly IUserService _userService;

    public NotificationsController(INotificationService notificationService, IUserService userService)
    {
        _notificationService = notificationService;
        _userService = userService;
    }

    /// <summary>US7: уведомления текущего пользователя (последние 50).</summary>
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMy()
    {
        var items = await _notificationService.ListUserNotificationsAsync(await CurrentUserAsync());
        return Ok(items.Select(n => n.ToDto()).ToList());
    }

    [HttpPut("{id:long}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(long id)
    {
        var notification = await _notificationService.MarkAsReadAsync(await CurrentUserAsync(), id);
        return Ok(notification.ToDto());
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationService.MarkAllAsReadAsync(await CurrentUserAsync());
        return NoContent();
    }

    private Task<Entities.User> CurrentUserAsync() => _userService.GetByIdAsync(User.GetUserId());
}
