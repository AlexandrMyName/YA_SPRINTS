using System.ComponentModel.DataAnnotations;
using Sprint1_Project_ASP_NetCore_API.Dtos;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
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
        }
    }

    /// <summary>
    /// Валидация выходных данных
    /// </summary>
    /// <param name="context"></param>
    public override void OnActionExecuted(ActionExecutedContext context)
    { 

        if (context.Exception != null || context.Result == null) return;
         
        if (context.Result is IApiResult objectResult)
        {
            var responseData = objectResult.GetData();
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
