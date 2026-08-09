using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings; 
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Services.DataServices;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Sprints_Project_ASP_NetCore_API.Services.DataServices;


namespace SprintASP_NetCore_API.Services.Background;


public class BookingBackgroundService : BackgroundService
{

    public BookingBackgroundService(IServiceScopeFactory scopeFactory, ILogger<BookingBackgroundService> logger )
    {
        _scopeFactory = scopeFactory;  
        _logger = logger;
    }

    private ILogger<BookingBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;  

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                 
                while (!stoppingToken.IsCancellationRequested)
                {

                    await Task.Delay(PollingInterval, stoppingToken);

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        // Получаем репозиторий для броней
                        var bookingRepo = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();
                        // И более высокоуровневый сервис:
                        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                        
                        // Работаем с репозиторием/сервисом
                        var bookings = await bookingRepo.GetAllAsync();
                         
                        var pendingBookings = await bookingService.GetPendingBookingsAsync();

                        if (pendingBookings.Count() > 0)
                        { 
                            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, bookingService, eventService, stoppingToken));
                            await Task.WhenAll(tasks);
                        }
                        else continue; 
                    } 
                }  
            }
            catch (OperationCanceledException operationCanceledExcept)
            {
                _logger.LogDebug("Работа BookingService была отменена: " + operationCanceledExcept.Message);
            }
            catch (Exception ex)
            {

                _logger.LogError("Вызвано исключение (BookingBackground): " + ex.Message);
            }
        }

        _logger.LogDebug("Работа BookingService завершена");
    }

    /// <summary>
    /// Метод подтверждения бронирования
    /// </summary>
    /// <param name="booking"></param>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    private async Task ProcessBookingAsync(IBookingInfoDto booking, IBookingService bookingService, IEventService eventService,  CancellationToken stoppingToken)
    {
        try
        {
  
            // 1. Имитация внешнего вызова (до семафора)
            await Task.Delay(ProcessingDelay, stoppingToken);

            // 2. Захват семафора
            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                // 3. Проверяем существование события
                var eventResult = await eventService.GetByIdAsync(booking.EventId);
                if (!eventResult.IsSuccesfuly || eventResult.Data == null)
                {
                    // Событие не найдено - отклоняем бронь
                    booking.Status = Data.Entities.BookingStatus.Rejected;
                    booking.ProcessedAt = DateTime.UtcNow;
                    await bookingService.UpdateBookingAsync(booking);
                    _logger.LogWarning($"Бронирование {booking.Id} отклонено: событие {booking.EventId} не найдено");
                    return;
                }

                var eventEntity = eventResult.Data;

                // 4. Подтверждаем бронь
                booking.Status = Data.Entities.BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.UtcNow;
                await bookingService.UpdateBookingAsync(booking);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // 5. Неожиданное исключение: отклоняем бронь, возвращаем место, обновляем хранилища 
                await eventService.ReleaseSeatsAndUpdateAsync(booking.EventId, count: 1);

                booking.Status = Data.Entities.BookingStatus.Rejected;
                booking.ProcessedAt = DateTime.UtcNow;
                await bookingService.UpdateBookingAsync(booking);
                _logger.LogError(ex, $"Ошибка при обработке брони {booking.Id}, бронь отклонена, места возвращены");
            }
            finally
            {
                // 6. Освобождаем семафор всегда
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug($"Обработка брони {booking.Id} отменена");
            // не пробрасываем, просто завершаем
        }
        catch (Exception ex)
        {
            // Ловим все остальные исключения, которые могли возникнуть до захвата семафора (например, Task.Delay)
            _logger.LogError(ex, $"Критическая ошибка при обработке брони {booking.Id}");
        }
    }
}
