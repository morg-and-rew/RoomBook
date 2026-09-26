using RoomBook.Api.Common;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Services;

/// <summary>
/// Уведомления (ФТ8) сохраняются в БД и отдаются через GET /api/notifications/my.
/// Канал email/push можно добавить здесь, не меняя вызывающий код.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationRepository notificationRepository, ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    public async Task NotifyStatusChangeAsync(Booking booking, NotificationType type)
    {
        var room = booking.Room.Name;
        var message = type switch
        {
            NotificationType.Created => $"Заявка на бронирование «{room}» создана и ожидает подтверждения.",
            NotificationType.Confirmed => $"Ваша заявка на «{room}» подтверждена администратором.",
            NotificationType.Rejected => $"Ваша заявка на «{room}» отклонена."
                + (booking.RejectReason is null ? string.Empty : $" Причина: {booking.RejectReason}."),
            NotificationType.Cancelled => $"Заявка на «{room}» отменена.",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        await _notificationRepository.SaveAsync(new Notification(booking.UserId, message, type, booking.Id));
        _logger.LogInformation("Уведомление ({Type}) для {UserId}: {Message}", type, booking.UserId, message);
    }

    public async Task NotifyUserAsync(User user, string message)
    {
        await _notificationRepository.SaveAsync(new Notification(user.Id, message));
        _logger.LogInformation("Уведомление для {UserId}: {Message}", user.Id, message);
    }

    public Task<IReadOnlyList<Notification>> ListUserNotificationsAsync(User user) =>
        _notificationRepository.FindByUserAsync(user);

    public async Task<Notification> MarkAsReadAsync(User user, long notificationId)
    {
        var notification = (await _notificationRepository.FindByUserAsync(user)).FirstOrDefault(n => n.Id == notificationId)
            ?? throw ApiException.NotFound("Уведомление не найдено.");
        notification.MarkAsRead();
        return await _notificationRepository.SaveAsync(notification);
    }

    public async Task MarkAllAsReadAsync(User user)
    {
        foreach (var notification in await _notificationRepository.FindByUserAsync(user))
        {
            if (notification.ReadAt is null)
            {
                notification.MarkAsRead();
                await _notificationRepository.SaveAsync(notification);
            }
        }
    }
}
