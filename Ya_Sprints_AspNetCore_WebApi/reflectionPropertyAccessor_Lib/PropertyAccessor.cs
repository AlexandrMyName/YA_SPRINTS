using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;


namespace reflectionPropertyAccessor_Lib;


/// <summary>
/// Класс доступа и кеширования методов рефлексии для получения и установки свойств по имени.
/// </summary>
public class PropertyAccessor
{

    // Хранилище аксессоров для каждой пары (accessorName, targetType)
    private static readonly ConcurrentDictionary<(string AccessorName, Type TargetType), PropertyAccessor> _accessors = new();

    // Кеш геттеров для текущего типа
    private readonly ConcurrentDictionary<string, Func<object, object>> _getters = new();

    // Кеш сеттеров для текущего типа
    private readonly ConcurrentDictionary<string, Action<object, object>> _setters = new();

    private PropertyAccessor() { }
     
    /// <summary>
    /// Получить делегат для чтения свойства.
    /// </summary>
    public static Func<object, object> GetPropertyGetter(string accessorName, Type targetType, string propertyName)
    {

        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        var accessor = _accessors.GetOrAdd((accessorName, targetType), _ => new PropertyAccessor());

        return accessor._getters.GetOrAdd(propertyName, name =>
        {
            var propInfo = targetType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null) throw new ArgumentException($"Свойство '{name}' не найдено в типе {targetType.Name}");

            // ❗️ Проверка на индексатор
            if (propInfo.GetIndexParameters().Length > 0) throw new ArgumentException($"Свойство '{name}' является индексатором и не поддерживается.");

            // Получить
            var objParam      = Expression.Parameter(typeof(object), "obj");
            var castObj       = Expression.Convert(objParam, targetType);
            var property      = Expression.Property(castObj, propInfo);
            var convertResult = Expression.Convert(property, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(convertResult, objParam);
            return lambda.Compile();
        });
    }
    
    /// <summary>
    /// Получить делегат для записи свойства.
    /// </summary>
    public static Action<object, object> GetPropertySetter(string accessorName, Type targetType, string propertyName)
    {

        if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

        var accessor = _accessors.GetOrAdd((accessorName, targetType), _ => new PropertyAccessor());

        return accessor._setters.GetOrAdd(propertyName, name =>
        {
            var propInfo = targetType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null)   throw new ArgumentException($"Свойство '{name}' не найдено в типе {targetType.Name}"); 
            if (!propInfo.CanWrite) throw new ArgumentException($"Свойство '{name}' в типе {targetType.Name} не имеет публичного сеттера");

            var objParam = Expression.Parameter(typeof(object), "obj");
            var valParam = Expression.Parameter(typeof(object), "value");
            var castObj = Expression.Convert(objParam, targetType);
            var castVal = Expression.Convert(valParam, propInfo.PropertyType);
            var property = Expression.Property(castObj, propInfo);
            var assign = Expression.Assign(property, castVal);
            var lambda = Expression.Lambda<Action<object, object>>(assign, objParam, valParam);
            return lambda.Compile();
        });
    }
}