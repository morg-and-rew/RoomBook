using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Common;
using RoomBook.Api.Dtos;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public UsersController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>Профиль текущего пользователя.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var user = await _userService.GetByIdAsync(User.GetUserId());
        return Ok(user.ToDto());
    }

    /// <summary>Изменение профиля. Возвращает новый токен: имя входит в его claims.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<AuthResponseDto>> UpdateMe(UpdateProfileDto dto)
    {
        var user = await _userService.GetByIdAsync(User.GetUserId());
        user = await _userService.UpdateProfileAsync(user, dto.FullName);
        var (token, expiresAt) = _tokenService.CreateToken(user);
        return Ok(new AuthResponseDto(token, expiresAt, user.ToDto()));
    }
}
