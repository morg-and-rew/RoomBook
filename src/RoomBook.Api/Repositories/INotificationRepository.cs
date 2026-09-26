using RoomBook.Api.Entities;

namespace RoomBook.Api.Repositories;

public interface INotificationRepository
{
    Task<Notification> SaveAsync(Notification notification);
    Task<IReadOnlyList<Notification>> FindByUserAsync(User user);
}
