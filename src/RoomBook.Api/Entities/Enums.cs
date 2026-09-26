namespace RoomBook.Api.Entities;

public enum UserRole
{
    User,
    Admin
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected,
    Cancelled
}

public enum NotificationType
{
    Created,
    Confirmed,
    Rejected,
    Cancelled
}
