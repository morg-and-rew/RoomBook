using Microsoft.EntityFrameworkCore;
using RoomBook.Api.Data;
using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

/// <summary>
/// Простая реализация уведомлений (ФТ8): сообщение сохраняется в БД
/// и отдаётся пользователю через GET /api/notifications/my.
/// Канал email/push можно добавить позже, не меняя вызывающий код —
/// достаточно расширить этот метод рассылкой через внешний провайдер.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid userId, string message)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message
        });
        await _db.SaveChangesAsync();
        _logger.LogInformation("Уведомление для {UserId}: {Message}", userId, message);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMyAsync(Guid userId)
    {
        var items = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return items.Select(n => new NotificationDto(n.Id, n.Message, n.IsRead, n.CreatedAt)).ToList();
    }
}
