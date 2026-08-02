using Microsoft.AspNetCore.Mvc;
using Sprints_Project_ASP_NetCore_API.Middlewares.Extentions.Configurations; 
using Sprints_Project_ASP_NetCore_API.Repositories.Extenions;
using Sprints_Project_ASP_NetCore_API.Services.Extentions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Sprints_Project_ASP_NetCore_API.Middlewares;
  

[assembly: ApiController] // Все контроллеры будут API 
 

namespace Sprints_Project_ASP_NetCore_API
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

            builder.Host.UseDefaultServiceProvider((context, options) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                }
            });

            var app = builder.Build();
            app.UseMiddleware<GlobalExceptionMiddleware>();  // Добавляет глобальный обработчик исключений 

            if (app.Environment.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage(); (При отладке раскоментировать)  (Если требуется стек вызовов) 
                app.UseSwagger();
                app.UseSwaggerUI(opt => { });   
                app.UseCors($"{CorsPoliticType.AllowAll}"); 
            }
            else
            { 
                app.UseCors($"{CorsPoliticType.Production}");
            }

            app.UseHttpsRedirection(); // Перенаправление на Https
            app.UseRouting();          // Анализ URL и вычисление конечного Endpoint (Без него маршрута к контроллеру не будет)   
            app.MapControllers();      // Использовать набор контроллеров 
            app.Run();
        }
    }
}


//// Модель запроса на генерацию отчёта
//public class ReportRequest
//{
//    public Guid Id { get; set; } = Guid.NewGuid();
//    public string ReportType { get; set; } = string.Empty;
//    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
//}

//// Сервис-очередь, регистрируется как Singleton
//public class ReportQueue
//{
//    private readonly Channel<ReportRequest> _channel =
//        Channel.CreateBounded<ReportRequest>(new BoundedChannelOptions(100)
//        {
//            FullMode = BoundedChannelFullMode.Wait
//        });

//    public async ValueTask EnqueueAsync(ReportRequest request, CancellationToken ct = default)
//    {
//        await _channel.Writer.WriteAsync(request, ct);
//    }

//    public IAsyncEnumerable<ReportRequest> ReadAllAsync(CancellationToken ct = default)
//    {
//        return _channel.Reader.ReadAllAsync(ct);
//    }
//}

//// Наследуемся от BackgroundService — ASP.NET Core сам создаст экземпляр
//// и вызовет ExecuteAsync при старте приложения
//public class ReportProcessingService : BackgroundService
//{
//    private readonly ReportQueue _queue;
//    private readonly ILogger<ReportProcessingService> _logger;

//    // Зависимости приходят через DI, как в обычном сервисе
//    public ReportProcessingService(ReportQueue queue, ILogger<ReportProcessingService> logger)
//    {
//        _queue = queue;
//        _logger = logger;
//    }

//    // Главный метод фонового сервиса. Вызывается один раз при старте приложения
//    // stoppingToken сработает, когда приложение начнёт останавливаться (Ctrl+C, деплой и т. д.)
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        await foreach (var request in _queue.ReadAllAsync(stoppingToken))
//        {
//            // Тайм-аут 30 секунд на одну задачу + остановка приложения
//            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
//            using var linkedCts = CancellationTokenSource
//                .CreateLinkedTokenSource(stoppingToken, timeoutCts.Token);

//            try
//            {
//                await ProcessReportAsync(request, linkedCts.Token);
//            }
//            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
//            {
//                _logger.LogWarning("Отчёт {Id}: тайм-аут обработки", request.Id);
//            }
//            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//            {
//                _logger.LogInformation("Остановка приложения, прерываем обработку");
//                break;
//            }
//        }
//    }
//}
//[ApiController]
//[Route("api/[controller]")]
//public class ReportsController : ControllerBase
//{
//    private readonly ReportQueue _queue;

//    public ReportsController(ReportQueue queue)
//    {
//        _queue = queue;
//    }

//    [HttpPost]
//    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest request)
//    {
//        var reportRequest = new ReportRequest
//        {
//            ReportType = request.ReportType
//        };

//        await _queue.EnqueueAsync(reportRequest);

//        return Ok(new { TaskId = reportRequest.Id, Message = "Отчёт поставлен в очередь" });
//    }
//}

//builder.Services.AddSingleton<ReportQueue>();
//builder.Services.AddHostedService<ReportProcessingService>();

//// Лог гонк потоков
//static void Log(string message) =>
//    Console.WriteLine(
//        $"{DateTime.Now:HH:mm:ss.fff} " +
//        $"| Поток: {Environment.CurrentManagedThreadId:00} " +
//        $"| {message}");