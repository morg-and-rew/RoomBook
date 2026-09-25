using System.Net;
using System.Text.Json;

namespace RoomBook.Api.Common;

/// <summary>
/// Единая точка обработки ошибок: любое исключение приводится
/// к общему формату ProblemDetails (код, сообщение, список полей).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(ex, "Обработанная ошибка API: {Message}", ex.Message);
            await WriteProblem(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка сервера");
            await WriteProblem(context, HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера.", null);
        }
    }

    private static async Task WriteProblem(HttpContext context, HttpStatusCode statusCode, string message,
        IDictionary<string, string[]>? errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = new
        {
            code = (int)statusCode,
            message,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
