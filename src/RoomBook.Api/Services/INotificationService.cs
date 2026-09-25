using RoomBook.Api.Dtos;

namespace RoomBook.Api.Services;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string message);
    Task<IReadOnlyList<NotificationDto>> GetMyAsync(Guid userId);
}
