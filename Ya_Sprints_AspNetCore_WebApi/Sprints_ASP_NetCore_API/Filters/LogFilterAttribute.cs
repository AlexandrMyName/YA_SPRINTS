using Microsoft.AspNetCore.Mvc.Filters;

namespace Sprint1_Project_ASP_NetCore_API.Filters
{
    public class LogFilterAttribute : ActionFilterAttribute
    {

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await next();
            stopwatch.Stop();
            Console.WriteLine($"Execution time: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
