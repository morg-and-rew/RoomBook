using Microsoft.AspNetCore.Mvc;
using RoomBook.Api.Dtos;
using RoomBook.Api.Entities;
using RoomBook.Api.Services;

namespace RoomBook.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public AuthController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>US2: регистрация нового пользователя.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var user = await _userService.RegisterAsync(dto.Email, dto.Password, dto.FullName);
        return Ok(BuildResponse(user));
    }

    /// <summary>US2: вход и выдача JWT.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
        return Ok(BuildResponse(user));
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var (token, expiresAt) = _tokenService.CreateToken(user);
        return new AuthResponseDto(token, expiresAt, user.ToDto());
    }
}
