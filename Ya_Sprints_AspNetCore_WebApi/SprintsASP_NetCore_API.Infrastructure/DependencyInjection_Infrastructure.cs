using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SprintASP_NetCore_API.Infrastructure.BackgroundServices;
using SprintASP_NetCore_API.Infrastructure.Repositories;
using SprintASP_NetCore_API.Services.Referencies;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintsASP_NetCore_API.Application.Abstractions.Security;
using SprintsASP_NetCore_API.Infrastructure.Concurrency;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.Interceptors;
using SprintsASP_NetCore_API.Infrastructure.Security;
 

namespace SprintsASP_NetCore_API.Infrastructure.DI;


public static class DependencyInjection_Infrastructure
{

    /// <summary>
    /// Регистрирует всё, что работает с внешним миром:
    /// БД, репозитории, интерцепторы, фоновые воркеры, системные сервисы.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // Персистентность  
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<AuditInterceptor>();

        services.AddSingleton<IPasswordHasher, PasswordHasher>();           
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();          

        // Concurrency (in-memory locks)  
        services.AddSingleton<IInterceptLockings, InterceptLockings>();

        // Системные сервисы  
        services.AddSingleton<IReferenciesData, RefDataService>();

        //  Background workers  
        services.AddHostedService<BookingBackgroundService>();


        //services.AddMassTransit(x =>
        //{
        //    x.AddConsumer<OrderCreatedConsumer>();
        //    //x.AddConsumer<HeavyOrderConsumer, HeavyOrderConsumerDefinition>();
             
        //    x.UsingRabbitMq((context, cfg) =>
        //    {
        //        cfg.Host("localhost", "/", h =>
        //        {
        //            h.Username("guest");
        //            h.Password("guest");
        //        });

        //        // Не перекладывать в _error — просто отбрасывать сообщение после исчерпания retry
        //        // cfg.DiscardFaultedMessages();


        //        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(10)));

        //        cfg.ConfigureEndpoints(context);
        //    });
        //});
         
        return services;
    }

    // Refit и Polly 
    // Паттерн SAGA и Паттерн Circuit Breaker (реализуется с помощью Polly) 


}

//public class HeavyOrderConsumerDefinition : ConsumerDefinition<HeavyOrderConsumer>
//{
//    protected override void ConfigureConsumer(
//        IReceiveEndpointConfigurator endpointConfigurator,
//        IConsumerConfigurator<HeavyOrderConsumer> consumerConfigurator,
//        IRegistrationContext context)
//    {
//        endpointConfigurator.UseMessageRetry(r =>
//            r.Exponential(5, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(3)));
//    }
//}
//// private readonly IPublishEndpoint _publishEndpoint;


//public record OrderCreated
//{
//    public Guid OrderId { get; init; }
//    public string CustomerEmail { get; init; }
//    public decimal TotalAmount { get; init; }
//}
//public class OrderCreatedConsumer : IConsumer<OrderCreated>
//{
//    private readonly ILogger<OrderCreatedConsumer> _logger;

//    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger)
//    {
//        _logger = logger;
//    }

//    public async Task Consume(ConsumeContext<OrderCreated> context)
//    {
//        var message = context.Message;

//        _logger.LogInformation(
//            "Получен заказ {OrderId} от {Email} на сумму {Amount}",
//            message.OrderId,
//            message.CustomerEmail,
//            message.TotalAmount);

//        // ... логика обработки заказа ...
//    }
//}
#region lesson

//public class InventoryClient
//{
//    private readonly HttpClient _httpClient = new();

//    // ВАЖНО: Политика объявляется как static (или Singleton в DI), 
//    // чтобы у всех запросов был общий счётчик ошибок
//    private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy =
//        Policy<HttpResponseMessage>
//            .Handle<HttpRequestException>() // Обработка сетевых исключений
//            .OrResult(r => (int)r.StatusCode >= 500) // Обработка кодов 5xx
//            .CircuitBreakerAsync(
//                handledEventsAllowedBeforeBreaking: 5, // Состояние Open после 5 ошибок подряд
//                durationOfBreak: TimeSpan.FromSeconds(30), // Время нахождения в состоянии Open
//                onBreak: (result, breakDelay) =>
//                {
//                    // Логика при переходе в состояние Open
//                    Console.WriteLine("Запросы заблокированы.");
//                },
//                onReset: () =>
//                {
//                    // Логика при возврате в состояние Closed
//                    Console.WriteLine("Работа возобновлена.");
//                },
//                // Это событие сработает при первой попытке выполнить запрос
//                // после истечения установленного времени (30 секунд)
//                onHalfOpen: () =>
//                {
//                    Console.WriteLine("Переход в HALF-OPEN: отправляем пробный запрос.");
//                }
//            );

//    public async Task GetItemsAsync()
//    {
//        // Пример выполнения запроса через политику
//        try
//        {
//            await _circuitBreakerPolicy.ExecuteAsync(async () =>
//                await _httpClient.GetAsync("http://inventory-service.com/api/items"));
//        }
//        catch (BrokenCircuitException)
//        {
//            // Исключение выбрасывается мгновенно, если состояние Open
//            Console.WriteLine("Запрос отклонен локально: удалённый сервис неисправен.");
//        }


//        // Пример Retry политики 

//        using Polly;

//        using var httpClient = new HttpClient();

//        // Описываем политику: обрабатываем только временные ошибки (5xx или ошибки сети)
//        var retryPolicy = Policy
//            .HandleResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
//            .Or<HttpRequestException>()
//            .WaitAndRetryAsync(3, retryAttempt =>
//                // Время ожидания растёт экспоненциально: 2, 4, 8 секунд
//                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
//                (outcome, timespan, retryCount, context) =>
//                {
//                    // Здесь можно залогировать номер попытки
//                    Console.WriteLine($"Попытка {retryCount} через {timespan.Seconds} сек.");
//                });

//        // Использование политики при вызове через HttpClient
//        await retryPolicy.ExecuteAsync(async () =>
//            await httpClient.GetAsync("http://inventory-service.com/api/items"))



//            Принцип идемпотентности и его роль в устойчивости систем


//            Пример реализации
//Так может выглядеть логика обработки платежа с защитой от дублей:

//public class PaymentService
//    {
//        private readonly IPaymentRepository _repository;

//        public PaymentService(IPaymentRepository repository) => _repository = repository;

//        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, Guid idempotencyKey)
//        {
//            // 1. Проверяем, был ли уже такой запрос
//            var existingPayment = await _repository.FindByKeyAsync(idempotencyKey);
//            if (existingPayment != null)
//            {
//                // Возвращаем старый результат без повторного списания денег
//                return new PaymentResult(existingPayment.Status, "Повторный запрос: оплата уже проведена");
//            }

//            // 2. Если запроса не было, выполняем логику списания
//            var result = await ExecuteTransactionAsync(request);

//            // 3. Сохраняем результат вместе с ключом идемпотентности
//            await _repository.SaveAsync(idempotencyKey, result);

//            return result;
//        }
//    }


// Анализ пограничных случаев
//При проектировании важно учитывать, что Polly работает внутри приложения и также потребляет ресурсы.Если создать тысячи отдельных политик для каждого мелкого вызова, потребление памяти вырастет.
//Лучшая практика — использовать общие экземпляры политик для вызовов к одному и тому же микросервису.

//// Одна политика Circuit Breaker на все запросы к сервису склада
//// Это позволяет всем потокам приложения одновременно узнать, что склад упал
//public class WarehouseIntegration
//    {
//        private static readonly IAsyncPolicy<HttpResponseMessage> _sharedPolicy =
//            (IAsyncPolicy<HttpResponseMessage>)Policy.Handle<HttpRequestException>()
//                  .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

//        public async Task GetProductAsync()
//        {
//            // Все вызовы используют одну статистику ошибок
//            await _sharedPolicy.ExecuteAsync(() => /* вызов через HttpClient */);
//        }
//    }



//    // Фоновый процесс для пересылки сообщений
//    public class OutboxProcessor : BackgroundService
//    {
//        private readonly IServiceProvider _services;
//        private readonly IMessageBroker _broker;

//        public OutboxProcessor(IServiceProvider services, IMessageBroker broker)
//        {
//            _services = services;
//            _broker = broker;
//        }

//        protected override async Task ExecuteAsync(CancellationToken token)
//        {
//            // Процесс работает в бесконечном цикле, пока запущено приложение
//            while (!token.IsCancellationRequested)
//            {
//                using var scope = _services.CreateScope();
//                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//                // Извлечение неотправленных сообщений из таблицы Outbox
//                var messages = await context.OutboxMessages
//                    .Where(m => !m.IsProcessed)
//                    .OrderBy(m => m.OccurredOn)
//                    .Take(20) // Обработка пачками для оптимизации нагрузки
//                    .ToListAsync(token);

//                foreach (var message in messages)
//                {
//                    try
//                    {
//                        // Отправка данных в брокер сообщений
//                        await _broker.PublishAsync(message.Type, message.Payload);

//                        // Фиксация успешной отправки в базе данных
//                        message.IsProcessed = true;
//                    }
//                    catch (Exception)
//                    {
//                        // При сбое в сети или брокере сообщение остаётся в базе 
//                        // для повторной попытки в следующем цикле
//                        break;
//                    }
//                }

//                await context.SaveChangesAsync(token);

//                // Интервал между проверками таблицы
//                await Task.Delay(TimeSpan.FromSeconds(1), token);
//            }
//        }
//    }




// Пример Inbox 


// using Npgsql; // Для работы с кодами ошибок Postgres

//public async Task HandleOrderPaidAsync(OrderPaidEvent message)
//{
//    try
//    {
//        // Сразу пытаемся добавить ID сообщения в таблицу Inbox
//        // Поле Id в этой таблице - Primary Key
//        _context.InboxMessages.Add(new InboxMessage
//        {
//            Id = message.Id,
//            ReceivedOn = DateTime.UtcNow
//        });

//        // Выполняем бизнес-логику (начисление баллов)
//        await _loyaltyService.AddPointsAsync(message.CustomerId, message.Amount);

//        // Фиксируем всё одной транзакцией
//        // Если такой ID уже есть, база выбросит исключение на этапе сохранения
//        await _context.SaveChangesAsync();
//    }
//    catch (DbUpdateException ex)
//        when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
//    {
//        // Если мы поймали ошибку уникальности PK - значит, это дубликат, 
//        // который уже обрабатывается или был обработан другим потоком
//        // Просто выходим без выполнения логики
//        return;
//    }
//}

#endregion