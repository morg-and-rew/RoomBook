using RoomBook.Api.Common;

namespace RoomBook.Api.Entities;

public class Booking
{
    private Booking() { } // для EF Core

    public Booking(User user, Room room, DateTime startAt, DateTime endAt, string? purpose)
    {
        if (!room.IsActive)
        {
            throw ApiException.NotFound("Помещение не найдено или недоступно для бронирования.");
        }
        if (endAt <= startAt)
        {
            throw new DomainException("Время окончания должно быть позже времени начала.");
        }

        User = user;
        UserId = user.Id;
        Room = room;
        RoomId = room.Id;
        StartAt = startAt;
        EndAt = endAt;
        Purpose = purpose?.Trim() ?? string.Empty;
    }

    public long Id { get; private set; }

    public long RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    public long UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string Purpose { get; private set; } = string.Empty;
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Pending;
    public string? RejectReason { get; private set; }

    /// <summary>Когда и кто из администраторов принял решение по заявке.</summary>
    public DateTime? DecidedAt { get; private set; }
    public long? DecidedById { get; private set; }
    public User? DecidedBy { get; private set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public void Confirm(User admin)
    {
        EnsureCanDecide(admin);
        Status = BookingStatus.Confirmed;
        MarkDecided(admin);
    }

    public void Reject(User admin, string? reason)
    {
        EnsureCanDecide(admin);
        Status = BookingStatus.Rejected;
        RejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        MarkDecided(admin);
    }

    /// <summary>Отменить заявку может её автор или администратор.</summary>
    public void Cancel(User by)
    {
        if (by.Id != UserId && !by.IsAdmin())
        {
            throw ApiException.Forbidden("Нельзя отменить чужую заявку.");
        }
        if (!IsActive())
        {
            throw new DomainException("Заявка уже отменена или отклонена.");
        }
        Status = BookingStatus.Cancelled;
    }

    /// <summary>Активная заявка занимает помещение: ожидает решения или подтверждена.</summary>
    public bool IsActive() => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    public bool OverlapsWith(Booking other) =>
        RoomId == other.RoomId
        && IsActive() && other.IsActive()
        && StartAt < other.EndAt && other.StartAt < EndAt;

    private void EnsureCanDecide(User admin)
    {
        if (!admin.IsAdmin())
        {
            throw ApiException.Forbidden("Решение по заявке принимает администратор.");
        }
        if (Status != BookingStatus.Pending)
        {
            throw new DomainException("Решение можно принять только по заявке в статусе «Ожидает подтверждения».");
        }
    }

    private void MarkDecided(User admin)
    {
        DecidedAt = DateTime.UtcNow;
        DecidedBy = admin;
        DecidedById = admin.Id;
    }
}
