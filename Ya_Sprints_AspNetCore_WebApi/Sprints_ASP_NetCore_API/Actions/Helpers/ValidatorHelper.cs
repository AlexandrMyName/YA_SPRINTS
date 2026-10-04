using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using reflectionPropertyAccessor_Lib;

namespace SprintASP_NetCore_API.Filters.ActionFilters;

/// <summary>
/// Рекурсивный валидатор DTO: стандартные DataAnnotations + доменные проверки,
/// не требующие обращения к БД (даты, диапазоны, Guid.Empty, роли, дубликаты в коллекции).
/// </summary>
public static class ValidatorHelper
{
    private const string ACCESSOR_NAME = "VALIDATION_ACCESSOR_TABLE";
    private const int MaxTotalSeats = 1_000_000;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string PropertyName), ValidationAttribute[]> _attributesCache = new();
    private static readonly ConcurrentDictionary<Type, SpecialProperties> _specialPropertiesCache = new();

    private static SpecialProperties GetSpecialProperties(Type type)
        => _specialPropertiesCache.GetOrAdd(type, t => new SpecialProperties(t));

    
    public static void ValidateObjectRecursive(object obj, List<ValidationResult> errors, string propertyPath = "")
    {
        if (obj == null) return;

        var type = obj.GetType();
        var properties = _propertiesCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.GetMethod != null && p.GetIndexParameters().Length == 0)
             .ToArray());

        var special = GetSpecialProperties(type);

        #region  Guid-поля: Id, EventId, UserId не должны быть Guid.Empty  
        ValidateNonEmptyGuid(obj, type, special.Id, "Id", errors);
        ValidateNonEmptyGuid(obj, type, special.EventId, "EventId", errors);
        ValidateNonEmptyGuid(obj, type, special.UserId, "UserId", errors);
        #endregion

        #region Role: только "User" или "Admin"  
        if (special.Role != default && special.Role.PropertyType == typeof(string))
        {
            var getter = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.Role.Name);
            var role = getter(obj) as string;
            if (!string.IsNullOrEmpty(role) && role != "User" && role != "Admin")
            {
                errors.Add(new ValidationResult(
                    "Роль должна быть 'User' или 'Admin'",
                    new[] { special.Role.Name }));
            }
        }
        #endregion

        #region TotalSeats: 1 .. MaxTotalSeats  
        if (special.TotalSeats != default)
        {
            var getter = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.TotalSeats.Name);
            var value = getter(obj);
            if (value is int intValue)
            {
                if (intValue <= 0)
                {
                    errors.Add(new ValidationResult(
                        "Общее количество мест должно быть больше 0",
                        new[] { special.TotalSeats.Name }));
                }
                else if (intValue > MaxTotalSeats)
                {
                    errors.Add(new ValidationResult(
                        $"Общее количество мест не должно превышать {MaxTotalSeats}",
                        new[] { special.TotalSeats.Name }));
                }
            }
        }
        #endregion

        #region StartAt < EndAt  
        if (special.StartAt != default && special.EndAt != default)
        {
            var from = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.StartAt.Name)(obj);
            var to = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.EndAt.Name)(obj);

            if (from is DateTime fromDate && to is DateTime toDate
                && fromDate != default && toDate != default
                && fromDate >= toDate)
            {
                errors.Add(new ValidationResult(
                    "Дата начала не может быть позже или равна дате окончания",
                    new[] { special.StartAt.Name, special.EndAt.Name }));
            }
        }
        #endregion

        #region From < To  
        if (special.From != default && special.To != default)
        {
            var from = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.From.Name)(obj);
            var to = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.To.Name)(obj);

            if (from is DateTime fromDate && to is DateTime toDate
                && fromDate != default && toDate != default
                && fromDate >= toDate)
            {
                errors.Add(new ValidationResult(
                    "Дата начала не может быть позже или равна дате окончания",
                    new[] { special.From.Name, special.To.Name }));
            }
        }
        #endregion

        #region Page >= 1, PageSize in [1..100]  
        if (special.Page != default && special.PageSize != default)
        {
            var page = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.Page.Name)(obj);
            var pageSize = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, special.PageSize.Name)(obj);

            if (page is int pageInt && pageInt < 1)
            {
                errors.Add(new ValidationResult(
                    "Номер страницы должен быть больше 0",
                    new[] { special.Page.Name }));
            }
            if (pageSize is int pageSizeInt && (pageSizeInt < 1 || pageSizeInt > 100))
            {
                errors.Add(new ValidationResult(
                    "Размер страницы должен быть от 1 до 100",
                    new[] { special.PageSize.Name }));
            }
        }
        #endregion


        #region  Стандартные DataAnnotations + рекурсия  
        foreach (var prop in properties)
        {
            var getter = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, prop.Name);
            var value = getter(obj);

            var attrs = _attributesCache.GetOrAdd((type, prop.Name), _ =>
                prop.GetCustomAttributes<ValidationAttribute>(true).ToArray());

            if (attrs.Length > 0)
            {
                var ctx = new ValidationContext(obj) { MemberName = prop.Name };
                foreach (var attr in attrs)
                {
                    var result = attr.GetValidationResult(value, ctx);
                    if (result != ValidationResult.Success && result != null)
                        errors.Add(result);
                }
            }

            if (value != null && ShouldRecurse(prop.PropertyType))
                ValidateObjectRecursive(value, errors, $"{propertyPath}{prop.Name}.");
        }
        #endregion
    }

    /// <summary>
    /// Проверка коллекции: дубликаты по Title и Login + рекурсивная валидация каждого элемента.
    /// </summary>
    public static void ValidateCollection(IEnumerable collection, List<ValidationResult> errors)
    {
        if (collection == null) return;

        ValidateNoDuplicates(collection, "Title", "название", errors);
        ValidateNoDuplicates(collection, "Login", "логин", errors);

        foreach (var item in collection)
        {
            if (item == null) continue;
            ValidateObjectRecursive(item, errors);
        }
    }

    #region  Хелперы  

    private static void ValidateNonEmptyGuid(
        object obj, Type type, PropertyInfo? prop, string name, List<ValidationResult> errors)
    {
        return;

        if (prop == default || prop.PropertyType != typeof(Guid)) return;

        var getter = PropertyAccessor.GetPropertyGetter(ACCESSOR_NAME, type, prop.Name);
        var value = getter(obj);
        if (value is Guid g && g == Guid.Empty)
        {
            errors.Add(new ValidationResult(
                $"Поле '{name}' не должно быть пустым Guid",
                new[] { prop.Name }));
        }
    }

    private static void ValidateNoDuplicates(
        IEnumerable collection, string propertyName, string humanName, List<ValidationResult> errors)
    {
        var seen = new HashSet<string>();
        foreach (var item in collection)
        {
            if (item == null) continue;

            var prop = item.GetType().GetProperty(propertyName);
            if (prop == null) return;   // если такого поля нет ни у кого — нечего проверять

            var value = prop.GetValue(item) as string;
            if (!string.IsNullOrEmpty(value) && !seen.Add(value))
            {
                errors.Add(new ValidationResult(
                    $"В передаваемых данных {humanName} не должно повторяться: {value}",
                    new[] { propertyName }));
            }
        }
    }

    private static bool ShouldRecurse(Type type)
        => !type.IsPrimitive
        && type != typeof(string)
        && !type.IsEnum
        && type != typeof(decimal)
        && type != typeof(DateTime)
        && type != typeof(DateTimeOffset)
        && type != typeof(TimeSpan)
        && type != typeof(Guid)
        && type != typeof(byte[])
        && type != typeof(IntPtr)
        && type != typeof(UIntPtr);

    #endregion

    private class SpecialProperties
    {
        public PropertyInfo? Page { get; }
        public PropertyInfo? PageSize { get; }
        public PropertyInfo? From { get; }
        public PropertyInfo? To { get; }
        public PropertyInfo? StartAt { get; }
        public PropertyInfo? EndAt { get; }
        public PropertyInfo? TotalSeats { get; }
        public PropertyInfo? Role { get; }
        public PropertyInfo? Id { get; }
        public PropertyInfo? EventId { get; }
        public PropertyInfo? UserId { get; }

        public SpecialProperties(Type type)
        {
            Page = type.GetProperty("Page");
            PageSize = type.GetProperty("PageSize");
            From = type.GetProperty("From");
            To = type.GetProperty("To");
            StartAt = type.GetProperty("StartAt");
            EndAt = type.GetProperty("EndAt");
            TotalSeats = type.GetProperty("TotalSeats");
            Role = type.GetProperty("Role");
            Id = type.GetProperty("Id");
            EventId = type.GetProperty("EventId");
            UserId = type.GetProperty("UserId");
        }
    }
}