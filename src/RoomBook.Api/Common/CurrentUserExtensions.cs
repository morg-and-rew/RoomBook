using System.Security.Claims;

namespace RoomBook.Api.Common;

public static class CurrentUserExtensions
{
    public static long GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (value is null || !long.TryParse(value, out var id))
        {
            throw ApiException.Unauthorized("Не удалось определить текущего пользователя.");
        }
        return id;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole("Admin");
}
