using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Tests.Helpers;


public static class FilterTestHelper
{
    /// <summary>
    /// Создает контекст для ActionExecutingContext
    /// </summary>
    public static ActionExecutingContext CreateActionExecutingContext(
        object controller,
        object? actionArgument = null,
        string actionName = "TestAction",
        ModelStateDictionary? modelState = null)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionContext = new ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor()
        );

        var actionArguments = new Dictionary<string, object?>();
        if (actionArgument != null)
        {
            actionArguments["dto"] = actionArgument;
        }

        var actionExecutingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            controller
        );

        if (modelState != null)
        {
            actionExecutingContext.ModelState.Clear();
            foreach (var key in modelState.Keys)
            {
                foreach (var error in modelState[key].Errors)
                {
                    actionExecutingContext.ModelState.AddModelError(key, error.ErrorMessage);
                }
            }
        }

        return actionExecutingContext;
    }

    /// <summary>
    /// Создает контекст для ActionExecutedContext
    /// </summary>
    public static ActionExecutedContext CreateActionExecutedContext(
        object controller,
        IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionContext = new ActionContext(
            httpContext,
            routeData,
            new ActionDescriptor()
        );

        return new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            controller
        )
        {
            Result = result
        };
    }
}