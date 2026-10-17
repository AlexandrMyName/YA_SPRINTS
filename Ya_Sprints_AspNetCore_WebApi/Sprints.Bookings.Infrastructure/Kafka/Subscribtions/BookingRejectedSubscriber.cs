using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Bookings.Application.Abstractions;
using Sprints.Bookings.Domain.Entities;
using Sprints.Bookings.Domain.ValueObjects;
using Sprints.Contracts;


namespace Sprints.Bookings.Infrastructure.Kafka.Subscribtions;
 

public class BookingRejectedSubscriber : BackgroundService
{

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _options;
    private readonly ILogger<BookingRejectedSubscriber> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BookingRejectedSubscriber(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<BookingRejectedSubscriber> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,       // "sprints-bookings"
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(KafkaTopics.BookingRejected);
        _logger.LogInformation("Kafka: Bookings подписан на {Topic}", KafkaTopics.BookingRejected);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(ct);
                    if (result?.Message?.Value == null) continue;

                    var message = JsonSerializer.Deserialize<BookingRejected>(
                        result.Message.Value, JsonOptions);

                    if (message == null) { consumer.Commit(result); continue; }

                    using var scope = _scopeFactory.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();

                    var found = repo.GetByIdAsync(message.BookingId, ct).GetAwaiter().GetResult();
                    if (!found.IsSuccesfuly || found.Data == null)
                    {
                        _logger.LogWarning(
                            "Bookings: бронь {BookingId} не найдена при BookingRejected",
                            message.BookingId);
                        consumer.Commit(result);
                        continue;
                    }

                    var booking = found.Data;

                    // Идемпотентность: если уже Rejected — пропускаем
                    if (booking.Status == BookingStatus.Rejected)
                    {
                        _logger.LogDebug("Bookings: бронь {BookingId} уже Rejected — skip", message.BookingId);
                        consumer.Commit(result);
                        continue;
                    }

                    // Rejected → Rejected (нельзя отменить уже отменённую)
                    if (booking.Status == BookingStatus.Cancelled)
                    {
                        _logger.LogWarning(
                            "Bookings: бронь {BookingId} уже Cancelled, BookingRejected игнорируется",
                            message.BookingId);
                        consumer.Commit(result);
                        continue;
                    }

                    booking.Reject();
                    repo.UpdateAsync(booking, ct).GetAwaiter().GetResult();
                    repo.SaveChangesAsync(ct).GetAwaiter().GetResult();

                    _logger.LogWarning(
                        "Bookings: бронь {BookingId} переведена в Rejected. Причина: {Reason}",
                        message.BookingId, message.Reason);

                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (ConsumeException ex) { _logger.LogError(ex, "Kafka: ошибка Consume"); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kafka: ошибка обработки BookingRejected");
                    if (result != null) consumer.Commit(result);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}