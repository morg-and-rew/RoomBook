using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;

namespace RoomBook.Api.Services;

public interface IBookingService
{
    Task<BookingDto> CreateAsync(Guid userId, BookingCreateDto dto);
    Task<IReadOnlyList<BookingDto>> GetMyBookingsAsync(Guid userId);
    Task<IReadOnlyList<BookingDto>> GetAllAsync(BookingStatus? status);
    Task<BookingDto> CancelAsync(Guid userId, Guid bookingId);
    Task<BookingDto> ApproveAsync(Guid bookingId);
    Task<BookingDto> RejectAsync(Guid bookingId, string? reason);
}
