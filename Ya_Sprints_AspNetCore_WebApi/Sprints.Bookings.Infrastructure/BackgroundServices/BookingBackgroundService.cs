using Sprints.Bookings.Application.UseCases.DataServices.Contracts;
using Sprints.Bookings.Application.Dtos.EntitiesDtos.Bookings;
using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using Sprints.Bookings.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sprints.Contracts;


namespace Sprints.Bookings.Infrastructure.BackgroundServices;


[Obsolete("Бронь подтверждается: сервисом событий, см. BookingAcceptedSubscriber")]
public class BookingBackgroundService : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingBackgroundService> _logger;

    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public BookingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
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

                var tasks = pendingList.Select(b => ProcessBookingAsync(b, stoppingToken));
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BookingBackgroundService: необработанная ошибка");
            }
        }
    }

    private async Task ProcessBookingAsync(
        IBookingInfoDto booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);
            await _processingSemaphore.WaitAsync(stoppingToken);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();

                // явная транзакция
                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                try
                { 
                    var entity = await db.Bookings.FindAsync(new object[] { booking.Id }, stoppingToken);
                    if (entity == null)
                    {
                        _logger.LogWarning("Bookings: бронь {BookingId} не найдена", booking.Id);
                        await tx.RollbackAsync(stoppingToken);
                        return;
                    }

                    // entity.Confirm();
                     
                    await outbox.EnqueueAsync(
                        topic: KafkaTopics.BookingRequested,
                        key: booking.EventId.ToString(),
                        payload: new BookingRequested
                        {
                            BookingId = booking.Id,
                            EventId = booking.EventId,
                            UserId = booking.UserId,
                            SeatsCount = 1,
                            RequestedAt = DateTime.UtcNow
                        },
                        ct: stoppingToken);

                    // Сохраняем ВСЁ в одной транзакции
                    await db.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);

                    _logger.LogInformation(
                        "Bookings: бронь {BookingId} подтверждена, добавлено в Outbox",
                        booking.Id);
                }
                catch
                {
                    await tx.RollbackAsync(stoppingToken);
                    throw;
                }
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