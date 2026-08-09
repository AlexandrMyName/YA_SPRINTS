using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings; 


namespace SprintASP_NetCore_API.Services.Background;


public class BookingBackgroundService : BackgroundService
{

    public BookingBackgroundService(ILogger<BookingBackgroundService> logger, IBookingService bookingService, IEventService eventStore)
    {
        _bookingStore = bookingService;
        _eventStore = eventStore;
        _logger = logger; 
    }

    private ILogger<BookingBackgroundService> _logger;
    private IBookingService _bookingStore;
    private IEventService _eventStore;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {

                var pendingBookings = await _bookingStore.GetPendingBookingsAsync();  

                if (pendingBookings.Count() > 0){
                      
                    var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                    await Task.WhenAll(tasks); 
                }
                else await Task.Delay(PollingInterval, stoppingToken); 
            }
            catch(OperationCanceledException operationCanceledExcept)
            {
                _logger.LogDebug("Работа BookingService была отменена: " + operationCanceledExcept.Message);
            }
            catch(Exception ex)
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
    private async Task ProcessBookingAsync(IBookingInfoDto booking, CancellationToken stoppingToken)
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
                var eventResult = await _eventStore.GetByIdAsync(booking.EventId);
                if (!eventResult.IsSuccesfuly || eventResult.Data == null)
                {
                    // Событие не найдено - отклоняем бронь
                    booking.Status = Data.Entities.BookingStatus.Rejected;
                    booking.ProcessedAt = DateTime.UtcNow;
                    await _bookingStore.UpdateBookingAsync(booking);
                    _logger.LogWarning($"Бронирование {booking.Id} отклонено: событие {booking.EventId} не найдено");
                    return;
                }

                var eventEntity = eventResult.Data;

                // 4. Подтверждаем бронь
                booking.Status = Data.Entities.BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.UtcNow;
                await _bookingStore.UpdateBookingAsync(booking);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // 5. Неожиданное исключение: отклоняем бронь, возвращаем место, обновляем хранилища 
                await _eventStore.ReleaseSeatsAndUpdateAsync(booking.EventId, count: 1);
               
                booking.Status = Data.Entities.BookingStatus.Rejected;
                booking.ProcessedAt = DateTime.UtcNow;
                await _bookingStore.UpdateBookingAsync(booking);
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
