using SprintASP_NetCore_API.Application.Filters;
using SprintASP_NetCore_API.Domain.Entities;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using SprintsASP_NetCore_API.Application.Abstractions;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты фильтрации и пагинации EventFilterDto.
/// </summary>
[Collection("DatabaseCollection")]
public class EventFilterTests : TestBase
{
    public EventFilterTests(DatabaseFixture fixture) : base(fixture) { }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearDatabaseAsync();

        var repository = GetService<IRepository<Event>>();

        var events = new List<Event>
        {
            Event.Create(Guid.NewGuid(), "Alpha", "Desc Alpha", DateTime.UtcNow,           DateTime.UtcNow.AddHours(1),           10),
            Event.Create(Guid.NewGuid(), "Beta",  "Desc Beta",  DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2),  5),
            Event.Create(Guid.NewGuid(), "Gamma", "Desc Gamma", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(3), 20),
            Event.Create(Guid.NewGuid(), "Delta", "Desc Delta", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(1),  1),
        };

        foreach (var ev in events)
            await repository.AddAsync(ev);

        await repository.SaveChangesAsync();

        // Устанавливаем AvailableSeats = 0 для "Delta"
        var delta = (await repository.GetAllAsync()).FirstOrDefault(e => e.Title == "Delta");
        if (delta != null)
        {
            delta.AvailableSeats = 0;
            await repository.UpdateAsync(delta);
            await repository.SaveChangesAsync();
        }
    }

    // ===================== Фильтр по Title (contains) =====================

    [Fact]
    public async Task FilterByTitle_Contains_ShouldReturnCorrectEvents()
    {
        var repository = GetService<IRepository<Event>>();

        var filter = new EventFilterDto
        {
            Title = "Al",
            Page = 1,
            PageSize = 10
        };

        var paged = await repository.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Alpha", paged.Items.First().Title);
    }

    // ===================== Фильтр по диапазону дат =====================

    [Fact]
    public async Task FilterByDateRange_ShouldReturnEventsWithinRange()
    {
        var repository = GetService<IRepository<Event>>();

        var from = DateTime.UtcNow.AddDays(1).AddMinutes(-10);
        var to = DateTime.UtcNow.AddDays(2).AddMinutes(10);

        var filter = new EventFilterDto
        {
            From = from,
            To = to,
            Page = 1,
            PageSize = 10
        };

        var paged = await repository.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(2, paged.TotalCount);
        Assert.Contains(paged.Items, e => e.Title == "Beta");
        Assert.Contains(paged.Items, e => e.Title == "Gamma");
    }

    // ===================== Сортировка по Title DESC =====================

    [Fact]
    public async Task SortByTitleDesc_ShouldReturnSortedEvents()
    {
        var repository = GetService<IRepository<Event>>();

        // Фильтр без условий — берём все, потом сортируем на клиенте
        var filter = new EventFilterDto { Page = 1, PageSize = 10 };

        var paged = await repository.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        var result = paged.Items
            .OrderByDescending(e => e.Title)
            .ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal("Gamma", result[0].Title);
        Assert.Equal("Delta", result[1].Title);
        Assert.Equal("Beta", result[2].Title);
        Assert.Equal("Alpha", result[3].Title);
    }

    // ===================== Пагинация =====================

    [Fact]
    public async Task Pagination_ShouldReturnCorrectPage()
    {
        var repository = GetService<IRepository<Event>>();

        // Страница 1: 2 элемента из 4
        var page1 = await repository.GetPagedAsync(
            predicate: null,
            page: 1,
            pageSize: 2);

        Assert.Equal(4, page1.TotalCount);

        var items1 = page1.Items.ToList();
        Assert.Equal(2, items1.Count);

        // Страница 2: оставшиеся 2 элемента
        var page2 = await repository.GetPagedAsync(
            predicate: null,
            page: 2,
            pageSize: 2);

        Assert.Equal(4, page2.TotalCount);

        var items2 = page2.Items.ToList();
        Assert.Equal(2, items2.Count);

        // Страницы не пересекаются
        var ids1 = items1.Select(e => e.Id).ToHashSet();
        var ids2 = items2.Select(e => e.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));

        // Вместе — все 4 события
        Assert.Equal(4, ids1.Union(ids2).Count());
    }

    // ===================== Фильтр AvailableSeats = 0 =====================

    [Fact]
    public async Task FilterByAvailableSeats_ShouldReturnEventsWithZeroAvailable()
    {
        var repository = GetService<IRepository<Event>>();

        var filter = new EventFilterDto
        {
            AvailableSeats = 0,
            Page = 1,
            PageSize = 10
        };

        var paged = await repository.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Delta", paged.Items.First().Title);
    }

    // ===================== Фильтр TotalSeats = 5 =====================

    [Fact]
    public async Task FilterByTotalSeats_ShouldReturnEventsWithExactNumber()
    {
        var repository = GetService<IRepository<Event>>();

        var filter = new EventFilterDto
        {
            TotalSeats = 5,
            Page = 1,
            PageSize = 10
        };

        var paged = await repository.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("Beta", paged.Items.First().Title);
    }
}