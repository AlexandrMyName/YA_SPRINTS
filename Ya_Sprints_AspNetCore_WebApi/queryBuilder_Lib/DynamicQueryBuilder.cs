using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;
using System;


namespace dynamicQueryBuilder;

// ============================================================
// 2. ОСНОВНОЙ КЛАСС – ПОСТРОИТЕЛЬ ДИНАМИЧЕСКИХ ЗАПРОСОВ
// ============================================================

/// <summary>
/// Базовый класс для динамического построения запросов к сущностям типа T.
/// Поддерживает фильтрацию, сортировку, группировку и проекцию.
/// Все операции строятся через деревья выражений (Expression Trees).
/// 
/// ОПТИМИЗАЦИЯ:
/// - Кеширование PropertyInfo (избавляет от повторного вызова GetProperty).
/// - Кеширование выражений сортировки/группировки (BuildSort).
/// - Потокобезопасные словари ConcurrentDictionary.
/// </summary>
/// <typeparam name="T">Тип сущности (например, Product).</typeparam>
public class DynamicQueryBuilder<T>
{

    #region КЕШ

    // ---------- КЕШИ ----------

    // Кеш PropertyInfo: ключ – имя свойства, значение – его метаданные.
    private static readonly ConcurrentDictionary<string, PropertyInfo> _propertyCache = new ConcurrentDictionary<string, PropertyInfo>();

    // Кеш для скомпилированных лямбда-выражений сортировки/группировки.
    // Ключ – имя свойства, значение – Expression<Func<T, object>>.
    private static readonly ConcurrentDictionary<string, Expression<Func<T, object>>> _sortLambdaCache = new ConcurrentDictionary<string, Expression<Func<T, object>>>(); 
    #endregion

    #region Публичные методы

    // ---------- ПУБЛИЧНЫЕ МЕТОДЫ ----------

    /// <summary>
    /// Строит предикат для фильтрации: x => x.Property operation value.
    /// Поддерживаемые операции: eq, ne, gt, lt, ge, le, contains, startswith, endswith, in, between.
    /// eq - равно
    /// == - равно
    /// ne - не равно
    /// != - не равно
    /// gt - больше
    /// > - больше
    /// lt - меньше
    /// < - меньше
    /// ge - больше или равно
    /// >= - больше или равно
    /// le - меньше или равно
    /// <= - меньше или равно
    /// contains - содержит
    /// startswith - начинается с
    /// endswith - заканчивается на
    /// in - в (принадлежит множеству)
    /// between - между (в диапазоне)
    /// </summary>
    public static Expression<Func<T, bool>> BuildFilter(string propertyName, string operation, object value)
    {

        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Имя свойства не может быть пустым.", nameof(propertyName));

        ParameterExpression param = Expression.Parameter(typeof(T), "x");  // определяем параметр - он же инстанс (тип свойства (тот же x в лямбда выражении)) 
        PropertyInfo propInfo = GetOrAddPropertyInfo(propertyName);        // получаем PropertyInfo 

        // Определение Assert
        if (propInfo == null) throw new ArgumentException($"Свойство '{propertyName}' не найдено в типе {typeof(T).Name}.", nameof(propertyName));

        MemberExpression propAccess = Expression.Property(param, propInfo); // определяем свойство x.(propertyName) 

        Expression body; // подготавливаем body выражения

        // Для "in" и "between" обрабатываем отдельно (массив значений)
        switch (operation.ToLowerInvariant())
        {
            case "in": // признак принадлежности множеству
                body = BuildInExpression(propAccess, value as IEnumerable<object>);
                break;
            case "between": // признак диапазона 
                body = BuildBetweenExpression(propAccess, value as object[]);
                break;
            default:

                // Для остальных операций преобразуем одиночное значение
                Expression valueExpr = ConvertToExpression(value, propInfo.PropertyType);

                body = operation.ToLowerInvariant() switch
                {
                    "eq" => Expression.Equal(propAccess, valueExpr),
                    "==" => Expression.Equal(propAccess, valueExpr),
                    "ne" => Expression.NotEqual(propAccess, valueExpr),
                    "!=" => Expression.NotEqual(propAccess, valueExpr),
                    "gt" => Expression.GreaterThan(propAccess, valueExpr),
                    ">"  => Expression.GreaterThan(propAccess, valueExpr),
                    "lt" => Expression.LessThan(propAccess, valueExpr),
                    "<"  => Expression.LessThan(propAccess, valueExpr),
                    "ge" => Expression.GreaterThanOrEqual(propAccess, valueExpr),
                    ">=" => Expression.GreaterThanOrEqual(propAccess, valueExpr),
                    "le" => Expression.LessThanOrEqual(propAccess, valueExpr),
                    "<=" => Expression.LessThanOrEqual(propAccess, valueExpr),
                    "contains" => BuildStringMethodCall(propAccess, "Contains", valueExpr),
                    "startswith" => BuildStringMethodCall(propAccess, "StartsWith", valueExpr),
                    "endswith" => BuildStringMethodCall(propAccess, "EndsWith", valueExpr),
                    _ => throw new NotSupportedException($"Операция '{operation}' не поддерживается.")
                };
                break;
        }
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    /// <summary>
    /// Применяет фильтр к IQueryable<T>.
    /// </summary>
    public static IQueryable<T> ApplyFilter(IQueryable<T> query, string propertyName, string operation, object value)
    {
        var filter = BuildFilter(propertyName, operation, value);
        return query.Where(filter);
    }

    /// <summary>
    /// Строит выражение сортировки: x => x.Property (с приведением к object).
    /// Результат кешируется по имени свойства – повторные вызовы не перестраивают дерево.
    /// </summary>
    public static Expression<Func<T, object>> BuildSort(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) throw new ArgumentException("Имя свойства не может быть пустым.", nameof(propertyName));

        return _sortLambdaCache.GetOrAdd(propertyName, name =>
        {
            var param = Expression.Parameter(typeof(T), "x");
            var propInfo = GetOrAddPropertyInfo(name);
            if (propInfo == null) throw new ArgumentException($"Свойство '{name}' не найдено в типе {typeof(T).Name}.", nameof(name));

            var propAccess = Expression.Property(param, propInfo);
            var convert = Expression.Convert(propAccess, typeof(object));
            return Expression.Lambda<Func<T, object>>(convert, param);
        });
    }

    /// <summary>
    /// Применяет сортировку к IQueryable (первичная).
    /// </summary>
    public static IQueryable<T> ApplySort(IQueryable<T> query, string propertyName, bool ascending = true)
    {
        var sortExpr = BuildSort(propertyName);
        return ascending
            ? Queryable.OrderBy(query, sortExpr)
            : Queryable.OrderByDescending(query, sortExpr);
    }

    /// <summary>
    /// Применяет дополнительную сортировку (ThenBy / ThenByDescending) к IOrderedQueryable.
    /// </summary>
    public static IOrderedQueryable<T> ApplyThenBy(IOrderedQueryable<T> query, string propertyName, bool ascending = true)
    {
        var sortExpr = BuildSort(propertyName);
        return ascending
            ? query.ThenBy(sortExpr)
            : query.ThenByDescending(sortExpr);
    }

    /// <summary>
    /// Строит выражение группировки: x => x.Property (использует тот же кеш, что и сортировка).
    /// </summary>
    public static Expression<Func<T, object>> BuildGroup(string propertyName) => BuildSort(propertyName); // переиспользуем

    /// <summary>
    /// Применяет группировку к IQueryable.
    /// </summary>
    public static IQueryable<IGrouping<object, T>> ApplyGroupBy(IQueryable<T> query, string propertyName)
    {
        var groupExpr = BuildGroup(propertyName);
        return query.GroupBy(groupExpr);
    }

    /// <summary>
    /// Строит выражение проекции: x => новый Dictionary<string, object>, 
    /// содержащий значения указанных свойств.
    /// </summary>
    public static Expression<Func<T, Dictionary<string, object>>> BuildProjection(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length == 0) throw new ArgumentException("Укажите хотя бы одно свойство.", nameof(propertyNames));

        var param = Expression.Parameter(typeof(T), "x");
        var dictType = typeof(Dictionary<string, object>);
        var addMethod = dictType.GetMethod("Add", new[] { typeof(string), typeof(object) });

        // Локальная переменная для словаря
        var dictVar = Expression.Variable(dictType, "dict");
        var newDict = Expression.New(dictType);
        var assignDict = Expression.Assign(dictVar, newDict);

        var expressions = new List<Expression> { assignDict };

        foreach (var pName in propertyNames)
        {
            var propInfo = GetOrAddPropertyInfo(pName);
            if (propInfo == null) throw new ArgumentException($"Свойство '{pName}' не найдено.", nameof(pName));

            var propAccess = Expression.Property(param, propInfo);
            var convert = Expression.Convert(propAccess, typeof(object));
            var key = Expression.Constant(pName);
            var addCall = Expression.Call(dictVar, addMethod, key, convert);
            expressions.Add(addCall);
        }

        expressions.Add(dictVar); // возвращаем словарь 
        var body = Expression.Block(new[] { dictVar }, expressions);

        return Expression.Lambda<Func<T, Dictionary<string, object>>>(body, param);
    }

    #endregion


    #region Вспомогательные методы 

    // ---------- ВСПОМОГАТЕЛЬНЫЕ ПРИВАТНЫЕ МЕТОДЫ ----------

    /// <summary>
    /// Получает PropertyInfo из кеша (если отсутствует – ищет через рефлексию и добавляет).
    /// </summary>
    private static PropertyInfo GetOrAddPropertyInfo(string propertyName)
        => _propertyCache.GetOrAdd(propertyName, name => typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance));

    /// <summary>
    /// Преобразует входное значение в константу выражения с нужным типом.
    /// Поддерживает преобразование типов (например, строка "123" в int).
    /// </summary>
    private static Expression ConvertToExpression(object value, Type targetType)
    {
        // Обработка null
        if (value == null) return Expression.Constant(null, targetType);

        var valueType = value.GetType();

        // Если значение уже совместимо с целевым типом (по ссылке или по значению)
        if (targetType.IsAssignableFrom(valueType)) return Expression.Constant(value, targetType);

        // Для Nullable<T> получаем базовый тип (T)
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;


        // Если значение реализует IConvertible – преобразуем через Convert.ChangeType
        if (value is IConvertible)
        {
            var converted = Convert.ChangeType(value, underlyingType);
            return Expression.Constant(converted, targetType);
        }

        // Если преобразование невозможно, но типы совместимы по наследованию – всё равно пробуем
        if (underlyingType.IsAssignableFrom(valueType)) return Expression.Constant(value, targetType);

        // Если ничего не подошло – выбрасываем исключение с деталями
        throw new InvalidOperationException(
            $"Не удалось преобразовать значение типа '{valueType.Name}' к типу '{targetType.Name}'. " +
            "Убедитесь, что значение реализует IConvertible или совместимо по наследованию."
        );
    }

    /// <summary>
    /// Вызывает строковый метод (Contains, StartsWith, EndsWith). 
    /// </summary>
    private static Expression BuildStringMethodCall(Expression instance, string methodName, Expression argument)
    {
        //var method = typeof(string).GetMethod(methodName, new[] { typeof(string) });
        //if (method == null) throw new NotSupportedException($"Метод '{methodName}' не найден в классе string.");
        //return Expression.Call(instance, method, argument);
         
        if (argument.Type != typeof(string)) throw new ArgumentException("Argument must be a string", nameof(argument));
         
        var method = typeof(string).GetMethod(
            methodName,
            new[] { typeof(string), typeof(StringComparison) }
        );

        if (method == null)
            throw new NotSupportedException($"Метод '{methodName}' с StringComparison не найден.");
         
        var comparison = Expression.Constant(StringComparison.OrdinalIgnoreCase); // Добавил OriginalIgnoreCase (регистро независимый метод)
         
        return Expression.Call(instance, method, argument, comparison);
    } 

    /// <summary>
    /// Строит выражение для IN: member == val1 || member == val2 || ...
    /// </summary>
    private static Expression BuildInExpression(Expression member, IEnumerable<object> values)
    {
        if (values == null || !values.Any()) return Expression.Constant(false);

        Expression orExpr = null;
        foreach (var val in values)
        {
            var valueConst = ConvertToExpression(val, member.Type);
            var equal = Expression.Equal(member, valueConst);
            orExpr = orExpr == null ? equal : Expression.OrElse(orExpr, equal);
        }
        return orExpr ?? Expression.Constant(false);
    }

    /// <summary>
    /// Строит выражение для BETWEEN: member >= lower && member <= upper.
    /// </summary>
    private static Expression BuildBetweenExpression(Expression member, object[] values)
    {
        if (values == null || values.Length < 2)
            throw new ArgumentException("Для Between требуется массив из двух значений (нижняя и верхняя границы).", nameof(values));

        var lower = ConvertToExpression(values[0], member.Type);
        var upper = ConvertToExpression(values[1], member.Type);
        var ge = Expression.GreaterThanOrEqual(member, lower);
        var le = Expression.LessThanOrEqual(member, upper);
        return Expression.AndAlso(ge, le);
    }

    #endregion 
}
