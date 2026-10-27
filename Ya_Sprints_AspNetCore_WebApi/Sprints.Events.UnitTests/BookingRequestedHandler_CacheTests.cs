using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprints.Contracts;
using Sprints.Events.Application.UseCases;
using Sprints.Events.Domain.Entities;
using Sprints.Events.UnitTests.TestData;
using Xunit;

namespace Sprints.Events.UnitTests;

/// <summary>
/// Тесты инвалидации кеша из асинхронного пути (Kafka → BookingRequestedHandler).
/// Ключевая идея: инвалидируем ТОЛЬКО когда сущность Event реально изменилась.
/// Ветки Rejected (нет мест / событие не найдено) и duplicate-skip (Inbox)
/// кеш не трогают — иначе thrashing при пиках отказов.
/// </summary>
public class BookingRequestedHandler_CacheTests
{
    // ============================================================
    // ACK #10: успешная бронь инвалидирует только events:event:{id}
    // ============================================================
    /// <summary>
    /// ACK #10
    /// Сценарий: успешная бронь (есть места).
    /// Ожидание: AvailableSeats уменьшился → инвалидируем events:event:{id}.
    /// Top-10 НЕ инвалидируем — TTL 60 c, иначе thrashing при потоке броней.
    /// Review: соответствие пункту задания «обновление данных через асинхронные события».
    /// </summary>
    [Fact]
    public async Task HandleAsync_SuccessfulBooking_InvalidatesEventCacheOnly()
    {
        // Given: есть 10 мест, бронируем 3 → успех
        var mocks = new BookingRequestedHandlerMocks();
        var eventId = Guid.NewGuid();
        var entity = EventBuilder.Create(id: eventId, totalSeats: 10, availableSeats: 10);

        mocks.Inbox
            .Setup(i => i.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Fail<InboxMessage>("not processed"));

        mocks.Events
            .Setup(e => e.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Ok(entity));

        var sut = CreateSut(mocks);

        // When
        await sut.HandleAsync(new BookingRequested
        {
            MessageId = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            EventId = eventId,
            SeatsCount = 3,
            UserId = Guid.NewGuid(),
        });

        // Then: единичный ключ инвалидирован
        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(eventId, It.IsAny<CancellationToken>()),
            Times.Once,
            "событие изменилось — кеш должен быть сброшен");

        // И: top-10 НЕ трогаем
        mocks.Cache.Verify(
            c => c.InvalidateTopEventsAsync(It.IsAny<CancellationToken>()),
            Times.Never,
            "top-10 не инвалидируется на бронь — защита от cache-thrashing");
    }

    // ============================================================
    // ACK #11: отказ «нет мест» не инвалидирует
    // ============================================================
    /// <summary>
    /// ACK #11 (часть 1)
    /// Сценарий: мест не хватает → BookingRejected.
    /// Ожидание: Event НЕ менялся → кеш не трогаем.
    /// Review: при массовом овербукинге это защищает от лавины RemoveAsync.
    /// </summary>
    [Fact]
    public async Task HandleAsync_RejectedNoSeats_DoesNotInvalidate()
    {
        // Given: 1 место, просят 5 → отказ
        var mocks = new BookingRequestedHandlerMocks();
        var eventId = Guid.NewGuid();
        var entity = EventBuilder.Create(id: eventId, totalSeats: 10, availableSeats: 1);

        mocks.Inbox
            .Setup(i => i.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Fail<InboxMessage>("not processed"));

        mocks.Events
            .Setup(e => e.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Ok(entity));

        var sut = CreateSut(mocks);

        // When
        await sut.HandleAsync(new BookingRequested
        {
            MessageId = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            EventId = eventId,
            SeatsCount = 5,
            UserId = Guid.NewGuid(),
        });

        // Then: кеш не тронут
        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "отказ без изменения сущности — инвалидация не нужна");
        mocks.Cache.Verify(
            c => c.InvalidateTopEventsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ============================================================
    // ACK #11: отказ «событие не найдено» не инвалидирует
    // ============================================================
    /// <summary>
    /// ACK #11 (часть 2)
    /// Сценарий: событие не найдено в БД → BookingRejected.
    /// Ожидание: кеш не трогаем (нечего инвалидировать).
    /// Review: случай «удалили событие, а Kafka ещё дошла».
    /// </summary>
    [Fact]
    public async Task HandleAsync_RejectedEventNotFound_DoesNotInvalidate()
    {
        // Given
        var mocks = new BookingRequestedHandlerMocks();

        mocks.Inbox
            .Setup(i => i.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Fail<InboxMessage>("not processed"));

        mocks.Events
            .Setup(e => e.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Fail<Event>("not found"));

        var sut = CreateSut(mocks);

        // When
        await sut.HandleAsync(new BookingRequested
        {
            MessageId = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SeatsCount = 1,
            UserId = Guid.NewGuid(),
        });

        // Then
        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "событие не найдено — инвалидировать нечего");
    }

    // ============================================================
    // ACK #12: дубль (Inbox hit) не инвалидирует
    // ============================================================
    /// <summary>
    /// ACK #12
    /// Сценарий: Inbox содержит MessageId → идемпотентный skip.
    /// Ожидание: ранний return, Event не читается, кеш не трогается.
    /// Review: at-least-once Kafka может доставить сообщение повторно —
    /// при этом мы не должны ни менять БД, ни дёргать Redis.
    /// </summary>
    [Fact]
    public async Task HandleAsync_DuplicateMessage_DoesNotInvalidate()
    {
        // Given: сообщение уже обработано ранее
        var mocks = new BookingRequestedHandlerMocks();

        mocks.Inbox
            .Setup(i => i.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Ok(
                InboxMessage.Create(Guid.NewGuid(), nameof(BookingRequested))));

        var sut = CreateSut(mocks);

        // When: повторная доставка того же MessageId
        await sut.HandleAsync(new BookingRequested
        {
            MessageId = Guid.NewGuid(),
            BookingId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SeatsCount = 1,
            UserId = Guid.NewGuid(),
        });

        // Then: кеш не тронут
        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "идемпотентный skip — сущность не менялась");

        // И: Event даже не читали из БД
        mocks.Events.Verify(
            e => e.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "дубль не должен доходить до UpdateAsync");
    }

    private static BookingRequestedHandler CreateSut(BookingRequestedHandlerMocks mocks) =>
        new(
            mocks.Events.Object,
            mocks.Inbox.Object,
            mocks.Outbox.Object,
            mocks.Locks.Object,
            mocks.Cache.Object,
            NullLogger<BookingRequestedHandler>.Instance);
}