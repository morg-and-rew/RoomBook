namespace RoomBook.Api.Entities;

public class Notification
{
    private Notification() { } // для EF Core

    public Notification(long userId, string message, NotificationType? type = null, long? bookingId = null)
    {
        UserId = userId;
        Message = message;
        Type = type;
        BookingId = bookingId;
    }

    public long Id { get; private set; }

    public long UserId { get; private set; }
    public User? User { get; private set; }

    /// <summary>Заявка, по которой отправлено уведомление (для общих сообщений — null).</summary>
    public long? BookingId { get; private set; }
    public Booking? Booking { get; private set; }

    /// <summary>Тип события по заявке; для общих сообщений (NotifyUser) — null.</summary>
    public NotificationType? Type { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; private set; }

    public void MarkAsRead() => ReadAt ??= DateTime.UtcNow;
}
