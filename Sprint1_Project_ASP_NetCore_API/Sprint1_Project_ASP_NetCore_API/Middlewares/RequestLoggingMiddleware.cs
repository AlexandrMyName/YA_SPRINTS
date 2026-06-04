namespace Sprint1_Project_ASP_NetCore_API.Middlewares;

public class RequestLoggingMiddleware
{

    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Логируем информацию о запросе ДО его обработки
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Request: {context.Request.Method} {context.Request.Path}");

        // Регистрируем callback для добавления заголовка перед началом отправки ответа
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Append("X-Custom-Header", "MyApp");
            return Task.CompletedTask;
        });

        // Вызываем следующий middleware в конвейере
        await _next(context);

        // Логируем статус ответа
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Response: {context.Response.StatusCode}");
    }
}
