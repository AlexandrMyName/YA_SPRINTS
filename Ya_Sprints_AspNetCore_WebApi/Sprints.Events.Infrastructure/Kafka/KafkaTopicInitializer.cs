using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Contracts;

namespace Sprints.Events.Infrastructure.Kafka;

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
                new TopicSpecification { Name = KafkaTopics.BookingRequested, NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = KafkaTopics.BookingAccepted,  NumPartitions = 3, ReplicationFactor = 1 },
                new TopicSpecification { Name = KafkaTopics.BookingRejected,  NumPartitions = 3, ReplicationFactor = 1 }
            });

            _logger.LogInformation("Kafka: топики созданы (или уже существуют)");
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Kafka: топики уже существуют");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka: не удалось создать топики");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}