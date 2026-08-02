using System.ComponentModel.DataAnnotations;
using reflectionPropertyAccessor_Lib;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections;

public static class ValidatorHelper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), ValidationAttribute[]> _attributesCache = new();
    private const string ACCESSOR_NAME = "VALIDATION_ACCESSOR_TABLE";

    private static readonly ConcurrentDictionary<Type, SpecialProperties> _specialPropertiesCache = new();
    private static SpecialProperties GetSpecialProperties(Type type) => _specialPropertiesCache.GetOrAdd(type, t => new SpecialProperties(t));

    public static void ValidateObjectRecursive(object obj, List<ValidationResult> errors, string propertyPath = "")
    {
        if (obj == null) return;

        var type = obj.GetType();
        var properties = _propertiesCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.GetMethod != null && p.GetIndexParameters().Length == 0)
             .ToArray()
        );

        var special = GetSpecialProperties(type);
        var pageProperty = special.Page;
        var pageSizeProperty = special.PageSize;
        var fromProperty = special.From;
        var toProperty = special.To;
        var startAtProperty = special.StartAt;
        var endAtProperty = special.EndAt;
        var totalSeatsProperty = special.TotalSeats;

        // Проверка TotalSeats
        if (totalSeatsProperty != default)
        {
            var getter = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, totalSeatsProperty.Name);
            var value = getter(obj);
            if (value is int intValue && intValue <= 0)
            {
                errors.Add(new ValidationResult("Общее количество мест должно быть больше 0", new[] { totalSeatsProperty.Name }));
                return;
            }
        }

        // Проверка StartAt/EndAt (только если оба не default)
        if (startAtProperty != default && endAtProperty != default)
        {
            var getterFrom = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, startAtProperty.Name);
            var getterTo = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, endAtProperty.Name);
            var from = getterFrom(obj);
            var to = getterTo(obj);

            if (from is DateTime fromDate && to is DateTime toDate && fromDate != default && toDate != default)
            {
                if (fromDate >= toDate)
                {
                    errors.Add(new ValidationResult(
                        "Дата начала не может быть позже или равна дате окончания",
                        new[] { startAtProperty.Name, endAtProperty.Name }));
                    return;
                }
            }
        }

        // Проверка From/To (только если оба не default)
        if (fromProperty != default && toProperty != default)
        {
            var getterFrom = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, fromProperty.Name);
            var getterTo = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, toProperty.Name);
            var from = getterFrom(obj);
            var to = getterTo(obj);

            if (from is DateTime fromDate && to is DateTime toDate && fromDate != default && toDate != default)
            {
                if (fromDate >= toDate)
                {
                    errors.Add(new ValidationResult(
                        "Дата начала не может быть позже или равна дате окончания",
                        new[] { fromProperty.Name, toProperty.Name }));
                    return;
                }
            }
        }

        // Проверка Page/PageSize
        if (pageProperty != default && pageSizeProperty != default)
        {
            var getterPage = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, pageProperty.Name);
            var getterPageSize = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, pageSizeProperty.Name);
            var page = getterPage(obj);
            var pageSize = getterPageSize(obj);

            if (page is int pageInt && pageSize is int pageSizeInt)
            {
                if (pageInt < 1)
                {
                    errors.Add(new ValidationResult("Номер страницы должен быть больше 0", new[] { pageProperty.Name }));
                }
                if (pageSizeInt > 100 || pageSizeInt < 1)
                {
                    errors.Add(new ValidationResult("Размер страницы должен быть от 1 до 100", new[] { pageSizeProperty.Name }));
                }
                // Не используем return, чтобы собрать все ошибки
            }
        }

        // Стандартные DataAnnotations
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

            if (value != null && ShouldRecurse(prop.PropertyType))
            {
                ValidateObjectRecursive(value, errors, $"{propertyPath}{prop.Name}.");
            }
        }
    }

    /// <summary>
    /// Проверяет коллекцию на дубликаты названий (свойство Title)
    /// </summary>
    public static void ValidateCollection(IEnumerable collection, List<ValidationResult> errors)
    {
        if (collection == null) return;
        var titles = new HashSet<string>();
        foreach (var item in collection)
        {
            if (item == null) continue;
            var titleProp = item.GetType().GetProperty("Title");
            if (titleProp != null)
            {
                var title = titleProp.GetValue(item) as string;
                if (!string.IsNullOrEmpty(title) && !titles.Add(title))
                {
                    errors.Add(new ValidationResult(
                        $"В передаваемых событиях название не должно повторяться: {title}",
                        new[] { "Title" }));
                }
            }
            // Рекурсивно проверяем каждый элемент
            ValidateObjectRecursive(item, errors);
        }
    }

    private static bool ShouldRecurse(Type type)
    {
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

    private class SpecialProperties
    {
        public PropertyInfo? Page { get; }
        public PropertyInfo? PageSize { get; }
        public PropertyInfo? From { get; }
        public PropertyInfo? To { get; }
        public PropertyInfo? StartAt { get; }
        public PropertyInfo? EndAt { get; }
        public PropertyInfo? TotalSeats { get; }

        public SpecialProperties(Type type)
        {
            Page = type.GetProperty("Page");
            PageSize = type.GetProperty("PageSize");
            From = type.GetProperty("From");
            To = type.GetProperty("To");
            StartAt = type.GetProperty("StartAt");
            EndAt = type.GetProperty("EndAt");
            TotalSeats = type.GetProperty("TotalSeats");
        }
    }
}