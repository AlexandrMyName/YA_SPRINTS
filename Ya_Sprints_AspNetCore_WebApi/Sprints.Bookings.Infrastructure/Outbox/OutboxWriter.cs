using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sprints.Bookings.Application.Abstractions;
using Sprints.Bookings.Domain.Entities;
using Sprints.Bookings.Infrastructure.DataAccess.DbContexts;
using System.Diagnostics;  

namespace Sprints.Bookings.Infrastructure.Outbox;

public class OutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _db;
    private readonly ILogger<OutboxWriter> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxWriter(AppDbContext db, ILogger<OutboxWriter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnqueueAsync<T>(
        string topic, string key, T payload, CancellationToken ct = default)
        where T : class
    {
        var messageId = Guid.NewGuid();

        var payloadWithId = payload switch
        {
            Sprints.Contracts.BookingRequested br => br with { MessageId = messageId },
            _ => throw new InvalidOperationException(
                $"Bookings.OutboxWriter: тип {typeof(T).Name} не поддерживает MessageId")
        };

        var json = JsonSerializer.Serialize(payloadWithId, JsonOptions);

        // Захватываем TraceId текущего HTTP-запроса
        var traceParent = Activity.Current?.Id;

        var message = OutboxMessage.Create(
            messageId: messageId,
            type: typeof(T).Name,
            topic: topic,
            key: key,
            payload: json,
            traceParent: traceParent); // Передаем в сущность

        await _db.OutboxMessages.AddAsync(message, ct);

        _logger.LogDebug(
            "Outbox: добавлено сообщение {MessageId} ({Type}) в {Topic}",
            messageId, typeof(T).Name, topic);
    }
}