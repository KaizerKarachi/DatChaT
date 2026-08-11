using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FamilyChat.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger) => 
        (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Необработанное исключение: {Path}", context.Request.Path);
            
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var error = new { error = "Внутренняя ошибка сервера", path = context.Request.Path };
            await context.Response.WriteAsJsonAsync(error);
        }
    }
}
