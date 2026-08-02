using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace SprintASP_NetCore_API.Filters.ActionFilters;

public class ValidateInputModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    { 

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ошибка валидации входных данных"
                });
            return;
        }

        // Бизнес-валидация входных аргументов
        var errors = new List<ValidationResult>();
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg == null) continue;

            if (arg is IEnumerable enumerable && arg is not string)
            { 
                ValidatorHelper.ValidateCollection(enumerable, errors);
            }
            else
            {
                ValidatorHelper.ValidateObjectRecursive(arg, errors);
            }
        }

        if (errors.Any())
        {
            var errorMessages = errors
                .Where(e => e != null)
                .Select(e => e!.ErrorMessage)
                .ToList();

            context.Result = new BadRequestObjectResult(new { Errors = errorMessages });
        }
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception != null || context.Result == null)
            return;

        if (context.Result is ObjectResult objectResult)
        {
            var responseData = objectResult.Value;
            var errors = new List<ValidationResult?>();

            if (responseData is IEnumerable enumerable && responseData is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item != null) ValidatorHelper.ValidateObjectRecursive(item, errors);
                }
            }
            else
            {
                ValidatorHelper.ValidateObjectRecursive(responseData, errors);
            }

            if (errors.Any())
            {
                var errorMessages = errors
                    .Where(e => e != null)
                    .Select(e => e!.ErrorMessage)
                    .ToList();

                context.Result = new BadRequestObjectResult(new { Errors = errorMessages });
            }
        }
    }
}