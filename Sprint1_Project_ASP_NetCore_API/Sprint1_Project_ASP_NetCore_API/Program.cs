using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Sprint1_Project_ASP_NetCore_API.Filters;
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions;
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations;
using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Endpoints;
using System.ComponentModel.DataAnnotations;


namespace Sprint1_Project_ASP_NetCore_API
{

    public class Program
    {

        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddLogFilterAttrib() 
                .AddCORS()
                .AddControllers(options =>
                { 
                    // Определяем профили кеширования
                    options.CacheProfiles.Add("Default", new CacheProfile
                    {
                        Duration = 60,
                        Location = ResponseCacheLocation.Any
                    });

                    options.CacheProfiles.Add("Never", new CacheProfile
                    {
                        NoStore = true,
                        Location = ResponseCacheLocation.None
                    }); 

                    options.CacheProfiles.Add("LongTerm", new CacheProfile
                    {
                        Duration = 3600, // 1 час
                        Location = ResponseCacheLocation.Any,
                        VaryByHeader = "Accept-Language"
                    });
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Конфигурация HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseCors("AllowAll");
            }
            else
            {
                app.UseCors("Production");  
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            // Подключаем наш custom middleware
            app.UseRequestLogging();

            app.UseAuthorization();

            app.MapProducts();
            app.MapControllers();

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



