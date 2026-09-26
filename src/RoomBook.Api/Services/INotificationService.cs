using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public interface INotificationService
{
    Task NotifyStatusChangeAsync(Booking booking, NotificationType type);
    Task NotifyUserAsync(User user, string message);

    // Дополнения к диаграмме: чтение уведомлений и отметка «прочитано» (Notification.markAsRead).
    Task<IReadOnlyList<Notification>> ListUserNotificationsAsync(User user);
    Task<Notification> MarkAsReadAsync(User user, long notificationId);
    Task MarkAllAsReadAsync(User user);
}
