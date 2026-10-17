  

namespace Sprints.Events.Infrastructure.Kafka;


public class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroup { get; set; } = "sprints-events";
}
