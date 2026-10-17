using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sprints.Bookings.Application.Abstractions;
 
using Sprints.Bookings.Application.UseCases.DataServices.Contracts;
using Sprints.Bookings.Application.Dtos.EntitiesDtos.Bookings;
using Sprints.Bookings.Domain.ValueObjects;
using Sprints.Contracts;


namespace Sprints.Bookings.Infrastructure.BackgroundServices;

public class BookingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaPublisher _kafka;
    private readonly ILogger<BookingBackgroundService> _logger;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        IKafkaPublisher kafka,
        ILogger<BookingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollingInterval, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                var pending = await bookingService.GetPendingBookingsAsync();
                var pendingList = pending.ToList();
                if (pendingList.Count == 0) continue;

                var tasks = pendingList.Select(b => ProcessBookingAsync(b, bookingService, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingBackgroundService: необработанная ошибка");
            }
        }

        _logger.LogDebug("BookingBackgroundService завершён");
    }

    private async Task ProcessBookingAsync(
        IBookingInfoDto booking,
        IBookingService bookingService,
        CancellationToken stoppingToken)
    {
        try
        {
            // имитация внешней проверки
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                // Меняем статус в СВОЕЙ БД
                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.UtcNow;

                await bookingService.UpdateBookingAsync(booking);   // SaveChanges внутри

                _logger.LogInformation("Bookings: бронь {BookingId} подтверждена", booking.Id);

                // Публикуем ТОЛЬКО ПОСЛЕ успешного сохранения
                var message = new BookingConfirmed
                {
                    BookingId = booking.Id,
                    EventId = booking.EventId,
                    UserId = booking.UserId,
                    SeatsCount = 1,
                    ConfirmedAt = booking.ProcessedAt.Value
                };

                await _kafka.PublishAsync(
                    topic: KafkaTopics.BookingConfirmed,
                    key: booking.EventId.ToString(),    // все брони одного события в один партишн
                    message,
                    stoppingToken);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Bookings: обработка {BookingId} отменена", booking.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bookings: ошибка обработки брони {BookingId}", booking.Id);
        }
    }
}