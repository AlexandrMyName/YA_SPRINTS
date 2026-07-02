using Microsoft.AspNetCore.Mvc.Filters;

namespace SprintASP_NetCore_API.Filters.ActionFilters
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
