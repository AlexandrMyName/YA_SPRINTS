 
using Sprints.Events.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Confluent.Kafka;
 

namespace Sprints.Events.Infrastructure.Kafka;


public sealed class KafkaPublisher : IKafkaPublisher, IDisposable
{

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };


    public KafkaPublisher(IOptions<KafkaOptions> options, ILogger<KafkaPublisher> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }


    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default)
        where T : class
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        var kafkaMessage = new Message<string, string> { Key = key, Value = payload };

        try
        {
            var delivery = await _producer.ProduceAsync(topic, kafkaMessage, ct);
            _logger.LogInformation(
                "Kafka: опубликовано в {Topic} [partition={Partition}, offset={Offset}], key={Key}",
                topic, delivery.Partition.Value, delivery.Offset.Value, key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka: ошибка публикации в {Topic}, key={Key}", topic, key);
            throw;
        }
    }


    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}