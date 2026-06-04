namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions;

public static class HttpLoggerMiddlewareExtensions
{
    /// <summary>
    /// Добавляет RequestLoggingMiddleware в конвейер обработки запросов
    /// </summary>
    public static IApplicationBuilder UseHttpLogger(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
