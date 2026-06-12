using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using System.ComponentModel.DataAnnotations;
using System.Reflection;


namespace Sprint1_Project_ASP_NetCore_API.Filters;


/// <summary>
/// ActionFilter - Валидация входных и выходных данных
/// </summary>
public class ValidateInputModelAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Валидация входных данных
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Возвращаем стандартную проблему валидации (RFC 7807)
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации входных данных"
            };
            context.Result = new BadRequestObjectResult(problem);
            return;
        }
         
        // Дополнительная бизнес-валидация для EventDto
        var dto = context.ActionArguments.Values.OfType<EventDto>().FirstOrDefault();
        if (dto != null && dto.StartAt >= dto.EndAt)
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
        }
        else
        {
            var dtos = context.ActionArguments.Values.OfType<IEnumerable<EventDto>>().FirstOrDefault();
            string errorStr = "";
            if (dtos == null) return;

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
                    modelState.AddModelError("", error); // пустой ключ = общая ошибка модели
                }
                context.Result = new BadRequestObjectResult(new ValidationProblemDetails(modelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ошибка валидации входных данных"
                });
            }


        }
    }

    /// <summary>
    /// Валидация выходных данных
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(ActionExecutedContext context)
    { 

        if (context.Exception != null || context.Result == null) return;
         
        if (context.Result is ObjectResult objectResult)
        {
            var responseData = objectResult.Value;
            var errors = new List<ValidationResult?>();

            // Валидация всех свойств объекта через рефлексию
            ValidateObjectRecursive(responseData, errors);

            if (errors.Any())
            {
                context.Result = new BadRequestObjectResult(new { 
                    Errors = errors.Select(e => e?.ErrorMessage) });
            }
        }
    }

    private void ValidateObjectRecursive(object obj, List<ValidationResult?> errors, string propertyPath = "")
    {

        if (obj == null) return;


        if (obj is EventDto eventDto)
        {

            if (eventDto.StartAt >= eventDto.EndAt)
            {
                errors.Add(new ValidationResult(
                        "Дата начала не может быть позже или равна дате окончания",
                        new[] { nameof(eventDto.StartAt), nameof(eventDto.EndAt) }
                    ));
                // Тут нужна ошибка
            }
        }
        else return;

        var type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            var validationAttributes = prop.GetCustomAttributes<ValidationAttribute>(true).ToList();

            if (validationAttributes.Any())
            {
                var validationContext = new ValidationContext(obj) { MemberName = prop.Name };
                foreach (var attr in validationAttributes)
                {
                    var result = attr.GetValidationResult(value, validationContext);
                    if (result != ValidationResult.Success)
                    {
                        errors.Add(result);
                    }
                }
            }
             
            if (value != null && !prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
            {
                ValidateObjectRecursive(value, errors, $"{propertyPath}{prop.Name}.");
            }
        }
    }
}
