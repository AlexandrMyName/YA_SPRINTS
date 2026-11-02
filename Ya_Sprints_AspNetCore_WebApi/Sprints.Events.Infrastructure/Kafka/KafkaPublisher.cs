using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Infrastructure.Kafka;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Sprints.Events.Infrastructure.Kafka; // Для Events измените namespace

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

        // ОБЫЧНЫЙ BUILD БЕЗ OpenTelemetry
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken ct = default) where T : class
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        await PublishRawAsync(topic, key, payload, ct);
    }

    public async Task PublishRawAsync(string topic, string key, string rawJson, CancellationToken ct = default)
    {
        var kafkaMessage = new Message<string, string> { Key = key, Value = rawJson };

        // ВРУЧНУЮ ДОБАВЛЯЕМ traceparent ДЛЯ СВЯЗИ ТРЕЙСОВ
        var activity = Activity.Current;
        if (activity != null && activity.Id != null)
        {
            kafkaMessage.Headers ??= new Headers();
            kafkaMessage.Headers.Add("traceparent", Encoding.UTF8.GetBytes(activity.Id));
        }

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
        try { _producer.Flush(TimeSpan.FromSeconds(5)); }
        catch (Exception ex) { _logger.LogWarning(ex, "Kafka: ошибка при Flush"); }
        finally { _producer.Dispose(); }
    }
}