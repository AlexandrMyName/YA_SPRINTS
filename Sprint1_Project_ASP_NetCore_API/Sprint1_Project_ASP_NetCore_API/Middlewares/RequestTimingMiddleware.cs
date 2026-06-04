

namespace Sprint1_Project_ASP_NetCore_API.Middlewares
{

    public class RequestTimingMiddleware
    {

        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext context)
        {
            // Логика ДО обработки запроса следующими middleware
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); 
            // Передача управления следующему middleware
            await _next(context); 
            // Логика ПОСЛЕ обработки запроса (когда ответ уже сформирован)
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds; 
            // Добавление заголовка с временем обработки
            context.Response.Headers.Add("X-Response-Time", $"{elapsed}ms");
        }
    }

    // public static class RequestTimingMiddleware_
}
