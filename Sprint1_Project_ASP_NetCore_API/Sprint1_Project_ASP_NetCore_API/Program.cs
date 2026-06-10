using Sprint1_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations; 
using Sprint1_Project_ASP_NetCore_API.Repositories.Extenions;
using Sprint1_Project_ASP_NetCore_API.Services.Extentions;
using Microsoft.AspNetCore.Mvc;
[assembly: ApiController] // Все контроллеры будут API 
 

namespace Sprint1_Project_ASP_NetCore_API
{

    public class Program
    {

        public static void Main(string[] args)
        {

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