using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.UseCases.DataServices;
using Sprints.Events.Domain.Entities;
using Sprints.Events.UnitTests.TestData;
using Xunit;

namespace Sprints.Events.UnitTests;

/// <summary>
/// Тесты стратегии кеширования в <see cref="EventsService"/>.
/// Покрывают: cache-aside для события по Id и топ-10, инвалидацию при
/// изменении/удалении, корректный порядок save→invalidate, отсутствие
/// инвалидации при ошибке репозитория.
/// </summary>
public class EventsService_CacheTests
{
    // ============================================================
    // ACK #1: GetByIdAsync — попадание в кеш
    // ============================================================
    /// <summary>
    /// ACK #1 / ACK #2
    /// Сценарий: событие есть в кеше.
    /// Ожидание: результат возвращается из кеша, репозиторий НЕ вызывается,
    /// повторная запись в кеш не выполняется (не перетираем сами себя).
    /// Review: ключевой тест требования «cache hit не обращается к БД».
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsCachedAndNeverHitsDb()
    {
        // Given: кеш содержит событие
        var mocks = new EventsServiceMocks();
        var id = Guid.NewGuid();
        var cached = EventBuilder.CreateDto(id: id, title: "From cache");

        mocks.Cache
            .Setup(c => c.GetEventAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var sut = CreateSut(mocks);

        // When: запрашиваем то же событие по Id
        var result = await sut.GetByIdAsync(id);

        // Then: результат из кеша, БД не тронута
        Assert.True(result.IsSuccesfuly);
        Assert.NotNull(result.Data);
        Assert.Equal("From cache", result.Data!.Title);

        mocks.Repository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "при cache hit в БД ходить нельзя");

        mocks.Cache.Verify(
            c => c.SetEventAsync(It.IsAny<EventInfoDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "на cache hit повторно в кеш не пишем — это лишний round-trip в Redis");
    }

    // ============================================================
    // ACK #2: GetByIdAsync — промах кеша
    // ============================================================
    /// <summary>
    /// ACK #2 (обратная сторона — cache miss)
    /// Сценарий: кеш пуст, событие есть в БД.
    /// Ожидание: идём в БД, заполняем кеш, возвращаем данные.
    /// Review: проверяет Cache-Aside — «get → miss → DB → set».
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_CacheMiss_FetchesDbAndPopulatesCache()
    {
        // Given: кеш пуст, в БД лежит событие
        var mocks = new EventsServiceMocks();
        var id = Guid.NewGuid();
        var entity = EventBuilder.Create(id: id, title: "From DB");

        mocks.Repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Ok(entity));

        mocks.Cache
            .Setup(c => c.GetEventAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInfoDto?)null);

        var sut = CreateSut(mocks);

        // When
        var result = await sut.GetByIdAsync(id);

        // Then: пришло из БД
        Assert.True(result.IsSuccesfuly);
        Assert.Equal("From DB", result.Data!.Title);

        // И: положили в кеш тот же Id
        mocks.Cache.Verify(
            c => c.SetEventAsync(
                It.Is<EventInfoDto>(d => d.Id == id),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "после cache miss событие должно быть закешировано");
    }

    // ============================================================
    // ACK #3: GetByIdAsync — промах + сущности нет → не кешируем negative
    // ============================================================
    /// <summary>
    /// ACK #3 (Negative caching — осознанно не кешируем)
    /// Сценарий: кеш пуст, в БД тоже нет.
    /// Ожидание: Fail, в кеш ничего не пишем.
    /// Review: важно для случая «удалённое событие потом создаётся с тем же Id» —
    /// негативный кеш дал бы stale-фейл на N секунд.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_CacheMissAndEntityAbsent_ReturnsFailAndDoesNotCache()
    {
        // Given: кеш пуст, в БД нет
        var mocks = new EventsServiceMocks();
        var id = Guid.NewGuid();

        mocks.Repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Fail<Event>("not found"));

        mocks.Cache
            .Setup(c => c.GetEventAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventInfoDto?)null);

        var sut = CreateSut(mocks);

        // When
        var result = await sut.GetByIdAsync(id);

        // Then: Fail, в кеш НЕ пишем
        Assert.False(result.IsSuccesfuly);
        mocks.Cache.Verify(
            c => c.SetEventAsync(It.IsAny<EventInfoDto>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "negative caching осознанно не применяем — см. README");
    }

    // ============================================================
    // ACK #4 / ACK #5: GetTopAsync — попадание в кеш
    // ============================================================
    /// <summary>
    /// ACK #4 / ACK #5
    /// Сценарий: топ-10 в кеше.
    /// Ожидание: возвращаем из кеша, БД не трогаем.
    /// Review: требование «cache hit top-10 не обращается к БД».
    /// </summary>
    [Fact]
    public async Task GetTopAsync_CacheHit_ReturnsCachedAndNeverHitsDb()
    {
        // Given
        var mocks = new EventsServiceMocks();
        var cachedTop = new List<EventInfoDto>
        {
            EventBuilder.CreateDto(title: "A"),
            EventBuilder.CreateDto(title: "B"),
        };

        mocks.Cache
            .Setup(c => c.GetTopEventsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedTop);

        var sut = CreateSut(mocks);

        // When
        var result = await sut.GetTopAsync(10);

        // Then
        Assert.Equal(2, result.Count);

        mocks.Repository.Verify(
            r => r.GetOrderedAsync(
                It.IsAny<Func<IQueryable<Event>, IOrderedQueryable<Event>>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "топ-10 из кеша — БД не трогаем");
    }

    // ============================================================
    // ACK #4: GetTopAsync — промах кеша
    // ============================================================
    /// <summary>
    /// ACK #4
    /// Сценарий: кеш пуст, в БД 10 событий.
    /// Ожидание: грузим из БД и кладём в кеш ПОЛНЫЙ топ-10.
    /// Review: в SetTopEventsAsync должен уйти список из 10 элементов,
    /// даже если БД вернула ровно 10 — на будущее обрезание по take.
    /// </summary>
    [Fact]
    public async Task GetTopAsync_CacheMiss_FetchesDbAndPopulatesCache()
    {
        // Given: 10 событий в БД
        var mocks = new EventsServiceMocks();
        var entities = Enumerable.Range(0, 10)
            .Select(i => EventBuilder.Create(title: $"Event {i}"))
            .ToList();

        mocks.Cache
            .Setup(c => c.GetTopEventsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EventInfoDto>?)null);

        mocks.Repository
            .Setup(r => r.GetOrderedAsync(
                It.IsAny<Func<IQueryable<Event>, IOrderedQueryable<Event>>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var sut = CreateSut(mocks);

        // When
        var result = await sut.GetTopAsync(10);

        // Then
        Assert.Equal(10, result.Count);
        mocks.Cache.Verify(
            c => c.SetTopEventsAsync(
                It.Is<IReadOnlyList<EventInfoDto>>(l => l.Count == 10),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "в кеш кладём полный топ-10");
    }

    // ============================================================
    // ACK #6: GetTopAsync — регрессия «take не портит кеш»
    // ============================================================
    /// <summary>
    /// ACK #6 (best practice, регрессия P0 из ревью)
    /// Сценарий: первый клиент просит take=3, второй — take=10.
    /// Ожидание: в кеш ВСЕГДА пишется полный топ-10; второй клиент получает 10.
    /// Review: без фикса первый вызов take=3 «отравил» бы кеш — второй клиент
    /// получил бы 3 элемента вместо 10. Тест ловит эту регрессию навсегда.
    /// </summary>
    [Fact]
    public async Task GetTopAsync_DifferentTakeValues_ShareSameFullCache()
    {
        // Given: пустой кеш, 10 событий в БД
        var mocks = new EventsServiceMocks();
        var entities = Enumerable.Range(0, 10)
            .Select(i => EventBuilder.Create(title: $"Event {i}"))
            .ToList();

        mocks.Cache
            .Setup(c => c.GetTopEventsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<EventInfoDto>?)null);

        mocks.Repository
            .Setup(r => r.GetOrderedAsync(
                It.IsAny<Func<IQueryable<Event>, IOrderedQueryable<Event>>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var sut = CreateSut(mocks);

        // When: первый клиент просит только 3
        var first = await sut.GetTopAsync(3);

        // Then: клиент получил 3 (обрезали на выдаче)
        Assert.Equal(3, first.Count);

        // НО: в кеш ушло 10, а не 3
        mocks.Cache.Verify(
            c => c.SetTopEventsAsync(
                It.Is<IReadOnlyList<EventInfoDto>>(l => l.Count == 10),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "в кеш всегда пишется полный топ-10, обрезаем только на выдаче");
    }

    // ============================================================
    // ACK #7: UpdateAsync — порядок save → invalidate-event → invalidate-top
    // ============================================================
    /// <summary>
    /// ACK #7
    /// Сценарий: успешный update события (меняет TotalSeats → популярность).
    /// Ожидание: сначала SaveChanges + Commit, потом инвалидация
    /// events:event:{id}, потом events:top10.
    /// Review: требование «порядок операций: сначала запись в БД, затем инвалидация».
    /// Проверяем через последовательность вызовов.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_OnSuccess_InvalidatesEventAndTop_InOrder()
    {
        // Given: успешный update в репозитории
        var mocks = new EventsServiceMocks();
        var id = Guid.NewGuid();
        var dto = EventBuilder.CreateDto(id: id, title: "updated", totalSeats: 50);
        var entity = EventBuilder.Create(id: id, title: "updated", totalSeats: 50);

        mocks.Repository
            .Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Ok(entity));

        // Лог последовательности вызовов
        var sequence = new List<string>();

        mocks.Repository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("save"))
            .ReturnsAsync(1);

        mocks.Cache
            .Setup(c => c.InvalidateEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("invalidate-event"))
            .Returns(Task.CompletedTask);

        mocks.Cache
            .Setup(c => c.InvalidateTopEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("invalidate-top"))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(mocks);

        // When
        var result = await sut.UpdateAsync(dto);

        // Then: успех
        Assert.True(result.IsSuccesfuly);

        // И: инвалидированы оба ключа
        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
        mocks.Cache.Verify(
            c => c.InvalidateTopEventsAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        // И: порядок строго save → invalidate-event → invalidate-top
        Assert.Equal(
            new[] { "save", "invalidate-event", "invalidate-top" },
            sequence);
    }

    // ============================================================
    // ACK #8: UpdateAsync — при ошибке БД кеш не трогаем
    // ============================================================
    /// <summary>
    /// ACK #8
    /// Сценарий: update упал на уровне репозитория.
    /// Ожидание: Fail, кеш НЕ инвалидируется.
    /// Review: инвалидация при ошибке = бесполезный round-trip в Redis
    /// и возможный cache-thrashing при серии фейлов.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_OnRepositoryFailure_DoesNotInvalidate()
    {
        // Given
        var mocks = new EventsServiceMocks();
        var dto = EventBuilder.CreateDto(title: "x");

        mocks.Repository
            .Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Fail<Event>("db error"));

        var sut = CreateSut(mocks);

        // When
        var result = await sut.UpdateAsync(dto);

        // Then
        Assert.False(result.IsSuccesfuly);
        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "при ошибке БД инвалидация не нужна — сущность не изменилась");
        mocks.Cache.Verify(
            c => c.InvalidateTopEventsAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ============================================================
    // ACK #9: DeleteAsync — инвалидация события и топа
    // ============================================================
    /// <summary>
    /// ACK #9
    /// Сценарий: успешное удаление.
    /// Ожидание: инвалидируем и events:event:{id}, и events:top10.
    /// Review: удалённое событие не должно висеть в топе до истечения TTL.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_OnSuccess_InvalidatesEventAndTop()
    {
        // Given
        var mocks = new EventsServiceMocks();
        var id = Guid.NewGuid();
        var entity = EventBuilder.Create(id: id);

        mocks.Repository
            .Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepoResults.Ok(entity));

        var sut = CreateSut(mocks);

        // When
        var result = await sut.DeleteAsync(id);

        // Then
        Assert.True(result.IsSuccesfuly);

        mocks.Cache.Verify(
            c => c.InvalidateEventAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
        mocks.Cache.Verify(
            c => c.InvalidateTopEventsAsync(It.IsAny<CancellationToken>()),
            Times.Once,
            "удалённое событие не должно появляться в топе до TTL");
    }

    private static EventsService CreateSut(EventsServiceMocks mocks) =>
        new(
            mocks.Repository.Object,
            mocks.Cache.Object,
            NullLogger<EventsService>.Instance,
            mocks.Locks.Object,
            mocks.Mapper.Object);
}