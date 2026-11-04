using System;

namespace Sprints.Bookings.Domain.Entities;

public class OutboxMessage : IEntity
{
    public Guid Id { get; set; }
    public string Type { get; private set; } = "";
    public string Topic { get; private set; } = "";
    public string Key { get; private set; } = "";
    public string Payload { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    // НОВОЕ: Храним traceparent для сквозной трассировки
    public string? TraceParent { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(
        Guid messageId, string type, string topic, string key, string payload, string? traceParent = null)
        => new()
        {
            Id = messageId,
            Type = type,
            Topic = topic,
            Key = key,
            Payload = payload,
            TraceParent = traceParent,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0
        };

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error;
    }
}