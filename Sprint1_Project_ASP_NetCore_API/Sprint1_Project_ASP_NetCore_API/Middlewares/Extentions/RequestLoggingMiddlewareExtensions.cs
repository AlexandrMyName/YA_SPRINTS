namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions;

public static class RequestLoggingMiddlewareExtensions
{
    /// <summary>
    /// Добавляет RequestLoggingMiddleware в конвейер обработки запросов
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
