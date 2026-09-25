using RoomBook.Api.Dtos;

namespace RoomBook.Api.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetAllAsync(DateTime? date, int? capacity, string[]? equipment);
    Task<RoomDto> CreateAsync(RoomCreateDto dto);
    Task<RoomDto> UpdateAsync(Guid id, RoomUpdateDto dto);
    Task DeleteAsync(Guid id);
}
