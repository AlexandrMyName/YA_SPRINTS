using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos; 
using System.ComponentModel.DataAnnotations;
using reflectionPropertyAccessor_Lib;
using System.Collections.Concurrent;
using System.Reflection;


public static class ValidatorHelper
{

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), ValidationAttribute[]> _attributesCache = new();
    private const string ACCESSOR_NAME = "VALIDATION_ACCESSOR_TABLE";


    public static void ValidateObjectRecursive(object obj, List<ValidationResult> errors, string propertyPath = "")
    {
        if (obj == null) return;

        // Специфичная проверка для EventDto (если нужна)
        if (obj is EventDto eventDto && eventDto.StartAt >= eventDto.EndAt)
        {
            errors.Add(new ValidationResult(
                "Дата начала не может быть позже или равна дате окончания",
                new[] { nameof(eventDto.StartAt), nameof(eventDto.EndAt) }
            ));
        }

        var type = obj.GetType();

        // Получаем свойства, исключая индексаторы (у них есть параметры)
        var properties = _propertiesCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.GetMethod != null && p.GetIndexParameters().Length == 0) // ← фильтр
             .ToArray()
        );

        foreach (var prop in properties)
        {
            var getter = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, prop.Name);
            var value = getter(obj);

            var attrs = _attributesCache.GetOrAdd((type, prop.Name), _ =>
                prop.GetCustomAttributes<ValidationAttribute>(true).ToArray()
            );

            if (attrs.Length > 0)
            {
                var context = new ValidationContext(obj) { MemberName = prop.Name };
                foreach (var attr in attrs)
                {
                    var result = attr.GetValidationResult(value, context);
                    if (result != ValidationResult.Success)
                        errors.Add(result);
                }
            }

            // Рекурсия только для сложных типов (исключаем строки, примитивы, перечисления)
            if (value != null && ShouldRecurse(prop.PropertyType))
            {
                ValidateObjectRecursive(value, errors, $"{propertyPath}{prop.Name}.");
            }
        }
    }

    private static bool ShouldRecurse(Type type)
    {
        // Исключаем все типы, которые не нужно обходить
        return !type.IsPrimitive &&
               type != typeof(string) &&
               !type.IsEnum &&
               type != typeof(decimal) &&
               type != typeof(DateTime) &&
               type != typeof(DateTimeOffset) &&
               type != typeof(TimeSpan) &&
               type != typeof(Guid) &&
               type != typeof(byte[]) &&
               type != typeof(IntPtr) &&
               type != typeof(UIntPtr);
    }
}