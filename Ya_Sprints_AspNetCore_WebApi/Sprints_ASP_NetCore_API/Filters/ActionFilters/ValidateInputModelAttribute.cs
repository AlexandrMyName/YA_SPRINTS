using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Filters;
using reflectionPropertyAccessor_Lib;
using Microsoft.AspNetCore.Mvc;
using System.Collections;


namespace SprintASP_NetCore_API.Filters.ActionFilters;


/// <summary>
/// ActionFilter - Валидация входных и выходных данных
/// </summary>
public class ValidateInputModelAttribute : ActionFilterAttribute
{
 
    /// <summary>
    /// Валидация входных данных
    /// </summary>
    public override void OnActionExecuting(ActionExecutingContext context)
    {

        // 1. Стандартная валидация ModelState
        if (!context.ModelState.IsValid)
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации входных данных"
            };
            context.Result = new BadRequestObjectResult(problem);
            return;
        }

        // 2. Бизнес-валидация для одиночного EventDto
        var singleDto = context.ActionArguments.Values.OfType<EventDto>().FirstOrDefault();
        if (singleDto != null)
        {
            if (!CheckInputData_EventDto(context, singleDto))
                return; // прерываем выполнение при ошибке
        }

        // 3. Бизнес-валидация для коллекции EventDto
        var collectionDtos = context.ActionArguments.Values.OfType<IEnumerable<EventDto>>().FirstOrDefault();
        if (collectionDtos != null)
        {
            if (!CheckInputData_EventDtos(context, collectionDtos)) return;
        }
    }

    private static bool CheckInputData_EventDtos(ActionExecutingContext context, IEnumerable<EventDto>? dtos)
    {

        if (dtos == null) return true;

        var errors = new List<string>();
        var titlesSet = new HashSet<string>();

        foreach (var d in dtos)
        {
            if (d.StartAt >= d.EndAt)
            {
                errors.Add($"Дата начала не может быть позже или равна дате окончания. Проверьте событие с названием: {d.Title}");
            }

            if (!titlesSet.Add(d.Title))
            {
                errors.Add($"В передаваемых событиях название не должно повторяться: {d.Title}");
            }
        }

        if (errors.Any())
        {
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
            {
                modelState.AddModelError("", error);
            }
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(modelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации входных данных"
            });
            return false;
        }
        return true;
    }

    private static bool CheckInputData_EventDto(ActionExecutingContext context, EventDto? dto)
    {
        if (dto == null) return true;

        if (dto.StartAt >= dto.EndAt)
        {
            var error = new ValidationResult(
                "Дата начала не может быть позже или равна дате окончания",
                new[] { nameof(dto.StartAt), nameof(dto.EndAt) }
            );
            var modelState = new ModelStateDictionary();
            modelState.AddModelError(error.MemberNames.First(), error.ErrorMessage);
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(modelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации входных данных"
            });
            return false; // ❗️ Исправлено: теперь возвращаем false при ошибке
        }
        return true;
    }

    /// <summary>
    /// Валидация выходных данных
    /// </summary>
    public override void OnActionExecuted(ActionExecutedContext context)
    {

        if (context.Exception != null || context.Result == null)
            return;

        if (context.Result is ObjectResult objectResult)
        {
            var responseData = objectResult.Value;
            var errors = new List<ValidationResult?>();

            // Валидируем либо один объект, либо каждый элемент коллекции
            if (responseData is IEnumerable enumerable && responseData is not string)
            {
                foreach (var item in enumerable)
                {
                    if (item != null) ValidatorHelper.ValidateObjectRecursive(item, errors);
                }
            }
            else ValidatorHelper.ValidateObjectRecursive(responseData, errors);
            
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


    [Obsolete("Логика перенесена в ValidatorHelper")]
    private void ValidateObjectRecursive(object obj, List<ValidationResult?> errors, string propertyPath = "")
        => ValidatorHelper.ValidateObjectRecursive(obj, errors, propertyPath);
}