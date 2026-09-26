using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Entities;
using RoomBook.Api.Repositories;

namespace RoomBook.Api.Data.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;

    public NotificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Notification> SaveAsync(Notification notification)
    {
        if (notification.Id == 0)
        {
            _db.Notifications.Add(notification);
        }
        await _db.SaveChangesAsync();
        return notification;
    }

    public async Task<IReadOnlyList<Notification>> FindByUserAsync(User user) =>
        await _db.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.SentAt)
            .Take(50)
            .ToListAsync();
}
