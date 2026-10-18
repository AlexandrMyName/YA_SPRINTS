using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;


namespace Sprints.Events.Infrastructure.Kafka;

/// <summary>
/// Создаёт топик booking-confirmed при старте, если его нет.
/// Запускается ДО BookingConfirmedSubscriber.
/// </summary>
public class KafkaTopicInitializer : IHostedService
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IOptions<KafkaOptions> options, ILogger<KafkaTopicInitializer> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _options.BootstrapServers
            }).Build();

            await admin.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = KafkaTopics.BookingConfirmed,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }
            });

            _logger.LogInformation("Kafka: топик {Topic} создан", KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka: топик {Topic} уже существует", KafkaTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka: не удалось создать топик {Topic}", KafkaTopics.BookingConfirmed);
            // не валим сервис
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}