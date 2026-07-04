using reflectionPropertyAccessor_Lib;
using System.Diagnostics;
using Xunit;


namespace Tests;

/// <summary>
/// Тест направленный на получение результатов оптимизации и без 
/// (Выводится в обозревателе тестов)
/// </summary>
public class Tests_Reflection
{

    private readonly ITestOutputHelper _output;
    public Tests_Reflection(ITestOutputHelper output) { _output = output; }
     

    public class TestData()
    {  
        public string? TestPropertyString { get; set; } = "123567890xz";
    }

    /// <summary>
    /// Метод проверяет затраченное количество времени получения/ назначения данных в свойства
    /// в свойства с помощью рефлексии 
    /// Сначала проверяется с применением кеширования бинарных выражений getProperty / SetProperty
    /// Далее тест проводится без использованием кеширования get/set
    /// Цикл тестирования: от 100  до 10 млн. итераций
    /// </summary> 
    [Theory]
    [InlineData(100)]        // мизерное количество
    [InlineData(1000)]       // маленькое количество
    [InlineData(100000)]     // среднее
    [InlineData(10000000)]   // большое 
    [InlineData(100000000)]  // огромное 
    public void PropertyAccessWithCache_ReturnsBetterThenWithoutCache(int iterations)
    {
        TestData data = new();

        // Прогрев JIT и кеша
        var getter = PropertyAccessor.GetPropertyGetter("TEST_ACCESSOR_TABLE", typeof(TestData), "TestPropertyString");
        var setter = PropertyAccessor.GetPropertySetter("TEST_ACCESSOR_TABLE", typeof(TestData), "TestPropertyString");

        getter(data);
        setter(data, "x");

        // Замер с кешем
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var v1 = getter(data);
            setter(data, "12889cx8e");
            var v2 = getter(data);
            setter(data, "123567890xz");
        }

        sw.Stop();
        double totalWithCache = sw.Elapsed.TotalMilliseconds;

        // Замер без кеша
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var prop = typeof(TestData).GetProperty("TestPropertyString");
            var v1 = prop.GetValue(data);
            prop.SetValue(data, "12889cx8e");
            var v2 = prop.GetValue(data);
            prop.SetValue(data, "123567890xz");
        }

        sw.Stop();
        double totalWithoutCache = sw.Elapsed.TotalMilliseconds;

        // Проверка корректности
        Assert.Equal("123567890xz", getter(data));
        // Вывод информации
        _output.WriteLine($"Итераций: {iterations}, с кешем: {totalWithCache:F2} мс, без кеша: {totalWithoutCache:F2} мс");
        // Сравнение
        Assert.True(totalWithCache < totalWithoutCache, $"С кешем: {totalWithCache:F2} мс, без кеша: {totalWithoutCache:F2} мс");
    }
}
