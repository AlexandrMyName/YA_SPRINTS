using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Net;
using Sprints_Project_ASP_NetCore_API.Data.Entities;


namespace Sprints_Project_ASP_NetCore_API.Middlewares;


public class GlobalExceptionMiddleware
{

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;


    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }


    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка: {Message}", ex.Message);

            // Если ответ уже начат – не можем его изменить, только логируем
            if (context.Response.HasStarted){
                _logger.LogWarning("Невозможно обработать ошибку, так как ответ уже начал передаваться клиенту.");
                // Завершаем обработку, чтобы не выбросить исключение повторно
                return;
            }  
            await HandleExceptionAsync(context, ex);
        }
    }


    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Определяем статус-код
        var statusCode = exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            NoAvailableSeatsException => (int) HttpStatusCode.Conflict ,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var problemDetails = new
        {
            Type     = "https://tools.ietf.org/html/rfc7807",
            Title    = "Ошибка обработки запроса",
            Status   = statusCode,
            Detail   = exception.Message,
            Instance = context.Request.Path,
        };

        // Очищаем существующий ответ и устанавливаем новый
        context.Response.Clear(); // важно, если что-то уже записано, но ответ ещё не начат
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return context.Response.WriteAsync(json);
    }
}