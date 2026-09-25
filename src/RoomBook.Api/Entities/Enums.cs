namespace RoomBook.Api.Entities;

public enum UserRole
{
    User = 0,
    Admin = 1
}

public enum BookingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}
