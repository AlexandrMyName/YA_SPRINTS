 
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations; 
using Sprint1_Project_ASP_NetCore_API.Repositories.Extenions;
using Sprint1_Project_ASP_NetCore_API.Services.Extentions;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Reflection;


[assembly: ApiController] // Все контроллеры будут API 
 

namespace Sprint1_Project_ASP_NetCore_API
{

    public class Program
    {

        public class TestClass1
        {
            public int Id { get; set; }

            public string Descript { get; set; }

            public string Name { get; set; }
        }

        public class TestClass2
        {
            public int Id { get; set; }

            public string TestStr { get; set; } 
        }


        public static void Main(string[] args)
        {
            // Вместо ручной регистрации каждой джобы:
            // services.AddScoped<SendEmailJob>();
            // services.AddScoped<ProcessOrderJob>();
            // services.AddScoped<GenerateReportJob>();

            // Автоматическая регистрация всех классов, реализующих IHangfireJob:
            //var jobTypes = Assembly.GetExecutingAssembly()
            //    .GetTypes()
            //    .Where(t => typeof(IHangfireJob).IsAssignableFrom(t)
            //                && !t.IsInterface
            //                && !t.IsAbstract);

            //foreach (var jobType in jobTypes)
            //{
            //    services.AddScoped(jobType);
            //}



            //var test1 = new List<TestClass1>()
            //{
            //    new(){ Descript = "des1", Id = 1, Name = "name1"},
            //    new(){ Descript = "des2", Id = 2, Name = "name2"},
            //    new(){ Descript = "des3", Id = 3, Name = "name3"},
            //};

            //var test2 = new List<TestClass2>()
            //{
            //    new(){ TestStr = "test1", Id = 1 },
            //    new(){ TestStr = "test2", Id = 2 },
            //    new(){ TestStr = "test3", Id = 3 },
            //};

            //var union = test1.Join(test2, one => one.Id, two => two.Id,
            //    (one, two) =>
            //    {
            //        return new { one.Name, two.TestStr };
            //    });

            //foreach(var u in union)
            //{
            //    Console.WriteLine(u);
            //}

            //while (true) { }
            var builder = WebApplication.CreateBuilder(args);

            builder.Services 
                .AddCorsPolicies() // Конфигурирование политики CORS
                .AddControllersWithCacheAndValidation() // Конфигурирование контроллеров с профилями кеширований и валидацией (ActionFilter)
                .AddEndpointsApiExplorer()   // Тестовые ендпоинты (minimal API) -> пока отключил
                .AddSwaggerGenWithDocumentation()  // Нужен для генерации метаданных Ыдля Swagger/Open Api 
                .AddApiVersioningCustom()  // Добавляет и конфигурирует версионирование АПИЫ
                .AddAutoMapper( typeof(Program))   // Добавляет автоматический маппинг моделей (Конфигурация в /ProfilesAndConfigs/MappingProfile находится по сборке автоматически) 
                .AddRepositories() // Добавляет репозитории в контейнер зависимостей
                .AddServices(); // Добавляет сервисы в контейнер зависимостей

            var app = builder.Build();
            
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(opt => {  });
                app.UseCors($"{CorsPoliticType.AllowAll}");

                builder.Host.UseDefaultServiceProvider(options =>
                {
                    // Проверяет Captive Dependency во время выполнения
                    options.ValidateScopes = true; 
                    // Проверяет корректность всех регистраций при старте приложения
                    options.ValidateOnBuild = true;
                }); 
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseCors($"{CorsPoliticType.Production}");
            }

            app.UseHttpsRedirection(); // Перенаправление на Https
            app.UseRouting();          // Анализ URL и вычисление конечного Endpoint (Без него маршрута к контроллеру не будет)   
            app.MapControllers();      // Использовать набор контроллеров 
            app.Run();
        }
    }
}



//// ПЛОХО: рефлексия в цикле обработки запросов
//public class BadRequestHandler
//{
//    public void ProcessRequest(HttpRequest request)
//    {
//        // Этот метод вызывается для КАЖДОГО запроса - тысячи раз в секунду!
//        Type handlerType = Type.GetType(request.HandlerName);
//        var handler = Activator.CreateInstance(handlerType); // Медленно!
//        var method = handlerType.GetMethod("Handle"); // Медленно!
//        method.Invoke(handler, new object[] { request }); // Медленно!
//    }
//}

//// ХОРОШО: кеширование или прямые вызовы
//public class GoodRequestHandler
//{
//    private readonly Dictionary<string, Func<HttpRequest, Task>> handlers = new();

//    public GoodRequestHandler()
//    {
//        // Регистрация обработчиков ОДИН РАЗ при инициализации
//        handlers["users"] = req => new UsersHandler().Handle(req);
//        handlers["orders"] = req => new OrdersHandler().Handle(req);
//    }

//    public Task ProcessRequest(HttpRequest request)
//    {
//        // Быстрый поиск в словаре и прямой вызов делегата
//        return handlers[request.HandlerName](request);
//    }
//}



//// Вместо рефлексии в runtime...
//public class ReflectionSerializer
//{
//    public string Serialize(object obj)
//    {
//        // Медленно: рефлексия при каждом вызове
//        var properties = obj.GetType().GetProperties();
//        // ...
//    }
//}

//// ...используем Source Generator, который генерирует код на этапе компиляции
//[AutoSerializable] // Атрибут для Source Generator
//public partial class User
//{
//    public string Name { get; set; }
//    public int Age { get; set; }
//}

//// Source Generator автоматически создаст метод:
//public partial class User
//{
//    public string Serialize()
//    {
//        // Быстро: прямой доступ к свойствам без рефлексии
//        return $"{{\"Name\":\"{Name}\",\"Age\":{Age}}}";
//    }
//}



////Expression Trees — компиляция делегатов
////Expression Trees позволяют создать делегат один раз и использовать многократно:

//public class PropertyAccessor<T>
//{
//    private static readonly Dictionary<string, Func<T, object>> getters = new();

//    public static Func<T, object> GetPropertyGetter(string propertyName)
//    {
//        if (getters.TryGetValue(propertyName, out var getter))
//            return getter;

//        // Создаём Expression Tree для доступа к свойству
//        var parameter = Expression.Parameter(typeof(T), "obj");
//        var property = Expression.Property(parameter, propertyName);
//        var convert = Expression.Convert(property, typeof(object));
//        var lambda = Expression.Lambda<Func<T, object>>(convert, parameter);

//        // Компилируем в делегат - медленно, но делаем ОДИН РАЗ
//        getter = lambda.Compile();
//        getters[propertyName] = getter;

//        return getter;
//    }
//}

//// Использование:
//var user = new User { Name = "Иван", Age = 30 };

//// Первый вызов: компиляция Expression Tree (~1000 нс)
//var nameGetter = PropertyAccessor<User>.GetPropertyGetter("Name");

//// Последующие вызовы: быстрый вызов делегата (~10 нс)
//string name = (string)nameGetter(user); // В 20 раз быстрее рефлексии!




//public static class PropertyAccessor<T>
//{
//    private static readonly ConcurrentDictionary<string, Func<T, object>> Getters = new();

//    public static Func<T, object> GetPropertyGetter(string propertyName)
//    {
//        if (string.IsNullOrEmpty(propertyName))
//            throw new ArgumentNullException(nameof(propertyName));

//        return Getters.GetOrAdd(propertyName, name =>
//        {
//            // Проверяем, что свойство существует
//            var propInfo = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
//            if (propInfo == null)
//                throw new ArgumentException($"Свойство '{name}' не найдено в типе {typeof(T).Name}");

//            var parameter = Expression.Parameter(typeof(T), "obj");
//            var property = Expression.Property(parameter, propInfo);
//            var convert = Expression.Convert(property, typeof(object));
//            var lambda = Expression.Lambda<Func<T, object>>(convert, parameter);
//            return lambda.Compile();
//        });
//    }
//}

//Решение: закешировать Type, MethodInfo и использовать compiled delegates или применить интерфейсы вместо рефлексии.