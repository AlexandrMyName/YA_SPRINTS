using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sprint1_Project_ASP_NetCore_API.Filters;
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions;
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Endpoints;
using System.ComponentModel.DataAnnotations;
using System.Net;

[assembly: ApiController] // Все контроллеры будут API 



namespace Sprint1_Project_ASP_NetCore_API
{

    public class Program
    {

        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddLogFilterAttrib()  // Конфигурирование аттрибута фильтрации для логирования  (Action Filter)
                .AddCorsPolicies() // Конфигурирование политики CORS
                .AddControllersWithCacheProfiles() // Конфигурирование контроллеров с профилями кеширований
                .AddEndpointsApiExplorer() // Нужен для генерации метаданных для Swagger/Open Api
                .AddSwaggerGenWithDocumentation() // Конфигурирование Swagger
                .AddApiVersioning();    // Добавляет верссионирование проекта

            var app = builder.Build();
            //builder.Services.AddScoped<object>(provider =>
            //{
            //    var config = provider.GetRequiredService<IConfiguration>();
            //    var connectionString = config.GetConnectionString("Orders");
            //    return Results.Ok();//  new  (connectionString);
            //});
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
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
            app.UseHttpLogger();       // Middleware Http логгера  
            app.UseAuthorization();    // Использовать систему авторизации 
            app.MapProducts();         // Использовать набор маленьких endpoints (ручек) для товаров  
            app.MapControllers();      // Использовать набор контроллеров 
            app.Run();
        }
    }
}

// Для конфигурации IOptions <> - Singltone 
// IOptionsSnapshot<T> — Scoped-настройки (Перезагружает Options при каждом подключении Get и т.д.) 

// Связываем секцию конфигурации с классом
//builder.Services.Configure<AppSettings>(
//    builder.Configuration.GetSection("AppSettings"));

//builder.Services.Configure<PaymentApiSettings>(
//    builder.Configuration.GetSection("ExternalServices:PaymentApi"));

// + Валидация настроек 
//public class PaymentApiSettings
//{
//    [Required, Url]
//    public string BaseUrl { get; set; }

//    [Range(1, 300)]
//    public int Timeout { get; set; }
//}

//builder.Services.AddOptions<PaymentApiSettings>()
//    .Bind(builder.Configuration.GetSection("ExternalServices:PaymentApi"))
//    .ValidateDataAnnotations()
//    .ValidateOnStart();

// Или так 
//builder.Services.AddOptions<PaymentApiSettings>()
//    .Bind(builder.Configuration.GetSection("ExternalServices:PaymentApi"))
//    .Validate(s => !string.IsNullOrEmpty(s.BaseUrl) && s.BaseUrl.StartsWith("https://"),
//    "BaseUrl должен использовать HTTPS")
//    .ValidateOnStart();

// Для сложной логики можно вынести валидацию
// IWebHostEnvironment - текущее окружение (ASPNETCORE_ENVIRONMENT)  (По умолчанию Production)

//public class PaymentApiSettingsValidator : IValidateOptions<PaymentApiSettings>
//{
//    public ValidateOptionsResult Validate(string name, PaymentApiSettings options)
//    {
//        if (string.IsNullOrEmpty(options.BaseUrl))
//            return ValidateOptionsResult.Fail("BaseUrl обязателен");

//        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
//            return ValidateOptionsResult.Fail("BaseUrl должен быть HTTPS URL");

//        return ValidateOptionsResult.Success;
//    }
//} 

//builder.Services.AddSingleton<IValidateOptions<PaymentApiSettings>, PaymentApiSettingsValidator>();

//IOptionsMonitor < T > — Реактивные настройки
//Отслеживает изменения в реальном времени.
//Можно подписаться на события изменения через OnChange.
//Работает в Singleton-сервисах.
//Используйте, когда нужна реакция на изменения без перезапуска.


// Регистрация именованного клиента с настройками
//builder.Services.AddHttpClient("StripeApi", client =>
//{
//    client.BaseAddress = new Uri("https://api.stripe.com/v1/");
//    client.Timeout = TimeSpan.FromSeconds(30);
//    client.DefaultRequestHeaders.Add("Accept", "application/json");
//    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
//});
// Это будет внедрено как IHttpClientFactory 
// У него есть метод CreateClient(с указанием имени - StripeApi) 

// Регистрация Typed HttpClient
// Можно так же явно прокинуть HttpClient с указанием в какой сервис его опрокинуть
//builder.Services.AddHttpClient<IPaymentService, StripePaymentService>(client =>
//{
//    client.BaseAddress = new Uri("https://api.stripe.com/v1/");
//    client.Timeout = TimeSpan.FromSeconds(30);
//})
//.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
//{
//    AutomaticDecompression = System.Net.DecompressionMethods.GZip
//});

// Регистрация - в основном это и пригодиться 
// services.Configure<EmailSettings>(configuration.GetSection("Email"));

//// Пример использования привязки из маршрута
//[HttpGet("{street}")]
//public Address GetAddress([FromRoute] string street)


 

 
 