

namespace Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions;

public static class RequestTimeMiddlewareExtention
{ 
    public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder builder)
        => builder.UseMiddleware<RequestTimingMiddleware>(); 
}

