using System.Reflection;


namespace Sprints.Events.UnitTests.TestData;

/// <summary>
/// Универсальный конструктор репозиторных результатов.
/// Ищет публичную статическую фабрику (Ok/Success/Create, Fail/Failure/Error)
/// и вызывает её. Избавляет от привязки тестов к конкретному имени метода.
/// </summary>
internal static class TestResults
{
    public static T Ok<T>(object data) where T : class
    {
        var method = FindFactory<T>("Ok", "Success", "Create", "Created", "SuccessData");
        return Invoke<T>(method, data);
    }

    public static T Fail<T>(string reason) where T : class
    {
        var method = FindFactory<T>("Fail", "Failure", "Error", "Failed");
        return Invoke<T>(method, reason);
    }

    private static MethodInfo FindFactory<T>(params string[] names)
    {
        var type = typeof(T);
        foreach (var name in names)
        {
            var candidates = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name && m.ReturnType == type);

            foreach (var m in candidates) return m;
        }

        throw new InvalidOperationException(
            $"Не найдена фабрика ({string.Join("/", names)}) на {type.FullName}. " +
            $"Открой исходник {type.Name} и добавь фабрику или поправь TestResults.cs.");
    }

    private static T Invoke<T>(MethodInfo method, object firstArg) where T : class
    {
        var pars = method.GetParameters();
        if (pars.Length == 0)
            throw new InvalidOperationException(
                $"Фабрика {method.DeclaringType?.Name}.{method.Name} без параметров — не подходит.");

        var args = new object?[pars.Length];
        args[0] = firstArg;
        for (var i = 1; i < pars.Length; i++)
        {
            args[i] = pars[i].HasDefaultValue
                ? pars[i].DefaultValue
                : (pars[i].ParameterType.IsValueType
                    ? Activator.CreateInstance(pars[i].ParameterType)
                    : null);
        }

        return (T)method.Invoke(null, args)!;
    }
}