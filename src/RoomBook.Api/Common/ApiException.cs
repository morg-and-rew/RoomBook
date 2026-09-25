using System.Net;

namespace RoomBook.Api.Common;

/// <summary>
/// Базовое исключение уровня приложения. StatusCode определяет,
/// какой HTTP-код и структуру ProblemDetails вернёт middleware.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        IDictionary<string, string[]>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public static ApiException NotFound(string message) => new(message, HttpStatusCode.NotFound);
    public static ApiException Conflict(string message) => new(message, HttpStatusCode.Conflict);
    public static ApiException Forbidden(string message) => new(message, HttpStatusCode.Forbidden);
    public static ApiException Unauthorized(string message) => new(message, HttpStatusCode.Unauthorized);
}
