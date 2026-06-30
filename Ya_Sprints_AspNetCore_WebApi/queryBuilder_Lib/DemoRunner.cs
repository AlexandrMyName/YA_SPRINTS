using System.Collections.Generic;
using System.Threading.Tasks;
using dynamicQueryBuilder;
using System.Linq;
using System.Text;
using System;


namespace queryBuilder_Lib
{

    // ============================================================
    // 1. МОДЕЛЬ ДАННЫХ (для демонстрации)
    // ============================================================

    /// <summary>
    /// Тестовый класс-сущность, с которым будем работать.
    /// </summary>
    public class TestDemoProduct
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ============================================================
    // 3. ДЕМОНСТРАЦИОННЫЙ КЛАСС – ПОКАЗ РАБОТЫ ВСЕХ МЕТОДОВ
    // ============================================================

    /// <summary>
    /// Содержит методы для демонстрации каждой возможности DynamicQueryBuilder.
    /// </summary>
    public static class DemoRunner
    {

        /// <summary>
        /// Формирует тестовый набор данных.
        /// </summary>
        private static IQueryable<TestDemoProduct> GetTestProducts()
        {
            var list = new List<TestDemoProduct>
            {
                new() { Id = 1, Name = "Laptop",   Price = 1200.50m, CategoryId = 1, CreatedAt = new DateTime(2023, 5, 10) },
                new() { Id = 2, Name = "Mouse",    Price = 25.99m,   CategoryId = 2, CreatedAt = new DateTime(2023, 6, 15) },
                new() { Id = 3, Name = "Keyboard", Price = 45.00m,   CategoryId = 2, CreatedAt = new DateTime(2023, 7, 20) },
                new() { Id = 4, Name = "Monitor",  Price = 300.00m,  CategoryId = 1, CreatedAt = new DateTime(2023, 8, 25) },
                new() { Id = 5, Name = "Tablet",   Price = 150.75m,  CategoryId = 3, CreatedAt = new DateTime(2023, 9, 30) }
            };
            return list.AsQueryable();
        }

        /// <summary>
        /// Демонстрация фильтрации: показывает все поддерживаемые операции.
        /// </summary>
        public static void DemoFiltering()
        {
            Console.WriteLine("===== 1. Демонстрация фильтрации =====");
            var query = GetTestProducts();

            // Равенство
            var filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(query, "CategoryId", "eq", 1);
            Console.WriteLine("  Товары с CategoryId = 1:");
            foreach (var p in filtered) Console.WriteLine($"    {p.Name}, Цена: {p.Price}"); // 

            // Равенство
            filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(filtered, "Name", "eq", "Laptop");
            foreach (var p in filtered) Console.WriteLine($" {p.Name} ");

            // Больше
            filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(query, "Price", "gt", 100);
            Console.WriteLine("  Товары дороже 100:");
            foreach (var p in filtered) Console.WriteLine($"    {p.Name}, Цена: {p.Price}");

            // Contains
            filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(query, "Name", "contains", "ap");
            Console.WriteLine("  Товары с 'ap' в названии:");
            foreach (var p in filtered) Console.WriteLine($"    {p.Name}");

            // IN
            filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(query, "CategoryId", "in", new object[] { 1, 3 });
            Console.WriteLine("  Товары из категорий 1 или 3:");
            foreach (var p in filtered) Console.WriteLine($"    {p.Name}, CategoryId: {p.CategoryId}");

            // BETWEEN
            filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(query, "Price", "between", new object[] { 40m, 200m });
            Console.WriteLine("  Товары с ценой от 40 до 200:");
            foreach (var p in filtered) Console.WriteLine($"    {p.Name}, Цена: {p.Price}");
            Console.WriteLine();
        }

        /// <summary>
        /// Демонстрация сортировки (одиночной и составной с ThenBy).
        /// </summary>
        public static void DemoSorting()
        {
            Console.WriteLine("===== 2. Демонстрация сортировки =====");
            var query = GetTestProducts();

            // Простая сортировка по возрастанию
            var sorted = DynamicQueryBuilder<TestDemoProduct>.ApplySort(query, "Price", ascending: true);
            Console.WriteLine("  Товары, отсортированные по цене (возрастание):");
            foreach (var p in sorted) Console.WriteLine($"    {p.Name}: {p.Price}");

            // Составная сортировка: по дате убыв., затем по имени возр.
            var ordered = (IOrderedQueryable<TestDemoProduct>)DynamicQueryBuilder<TestDemoProduct>.ApplySort(query, "CreatedAt", false);
            ordered = DynamicQueryBuilder<TestDemoProduct>.ApplyThenBy(ordered, "Name", true);
            Console.WriteLine("  Товары, отсортированные по дате (убыв.) и затем по имени (возр.):");
            foreach (var p in ordered) Console.WriteLine($"    {p.Name}, Дата: {p.CreatedAt:yyyy-MM-dd}");
            Console.WriteLine();
        }

        /// <summary>
        /// Демонстрация группировки по полю.
        /// </summary>
        public static void DemoGrouping()
        {
            Console.WriteLine("===== 3. Демонстрация группировки =====");
            var query = GetTestProducts();
            var grouped = DynamicQueryBuilder<TestDemoProduct>.ApplyGroupBy(query, "CategoryId");

            Console.WriteLine("  Группировка товаров по CategoryId:");
            foreach (var group in grouped)
            {
                Console.WriteLine($"    Категория {group.Key}:");
                foreach (var p in group)
                    Console.WriteLine($"      {p.Name} - {p.Price}");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Демонстрация проекции – выборка только нужных полей в Dictionary.
        /// </summary>
        public static void DemoProjection()
        {
            Console.WriteLine("===== 4. Демонстрация проекции =====");
            var query = GetTestProducts();

            var projectionExpr = DynamicQueryBuilder<TestDemoProduct>.BuildProjection("Id", "Name", "Price");
            var projected = query.Select(projectionExpr).ToList();

            Console.WriteLine("  Результат проекции (Id, Name, Price):");
            foreach (var dict in projected)
                Console.WriteLine($"    Id: {dict["Id"]}, Name: {dict["Name"]}, Price: {dict["Price"]}");

            // Комбинируем с фильтрацией и сортировкой
            var filtered = DynamicQueryBuilder<TestDemoProduct>.ApplyFilter(query, "Price", "gt", 100);
            var sorted = DynamicQueryBuilder<TestDemoProduct>.ApplySort(filtered, "Price", false);
            var final = sorted.Select(projectionExpr).ToList();

            Console.WriteLine("  Товары дороже 100, отсортированные по убыванию цены (Id, Name, Price):");
            foreach (var dict in final)
                Console.WriteLine($"    Id: {dict["Id"]}, Name: {dict["Name"]}, Price: {dict["Price"]}");
            Console.WriteLine();
        }

        /// <summary>
        /// Демонстрация работы кеширования (BuildSort повторно использует уже построенное выражение).
        /// </summary>
        public static void DemoCache()
        {
            Console.WriteLine("===== 5. Демонстрация кеширования =====");

            var expr1 = DynamicQueryBuilder<TestDemoProduct>.BuildSort("Price");
            var expr2 = DynamicQueryBuilder<TestDemoProduct>.BuildSort("Price");
            Console.WriteLine($"  expr1 и expr2 ссылаются на один объект? {ReferenceEquals(expr1, expr2)}");

            // Замер производительности с кешем
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                var _ = DynamicQueryBuilder<TestDemoProduct>.BuildSort("Price");
            }
            sw.Stop();
            Console.WriteLine($"  100 000 вызовов BuildSort с кешем заняло {sw.ElapsedMilliseconds} мс");

            // Для сравнения – без кеша пришлось бы искать PropertyInfo каждый раз,
            // но мы его тоже кешируем, поэтому всё работает быстро.
            Console.WriteLine("  (Кеш PropertyInfo также задействован – повторные GetProperty не происходят)");
            Console.WriteLine();
        }

        /// <summary>
        /// Запускает все демонстрации по порядку.
        /// </summary>
        public static void RunAll()
        {
            DemoFiltering();
            DemoSorting();
            DemoGrouping();
            DemoProjection();
            DemoCache();
        }
    }
}
