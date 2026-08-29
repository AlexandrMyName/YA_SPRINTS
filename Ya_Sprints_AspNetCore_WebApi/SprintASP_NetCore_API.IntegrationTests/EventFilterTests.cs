using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Repositories;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для проверки фильтрации с библиотекой queryBuilder.lib
/// Проверяют EventFilterDto
/// </summary>
public class EventFilterTests : TestBase
{

    public EventFilterTests()
    {
        
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearDatabaseAsync();

        var repository = GetService<IRepository<Event>>();

        // Создаём тестовые события
        var events = new List<Event>
        {
            Event.Create(Guid.NewGuid(), "Alpha", "Desc Alpha", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10),
            Event.Create(Guid.NewGuid(), "Beta", "Desc Beta", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 5),
            Event.Create(Guid.NewGuid(), "Gamma", "Desc Gamma", DateTime.UtcNow.AddDays(2), DateTime.UtcNow.AddDays(2).AddHours(3), 20),
            Event.Create(Guid.NewGuid(), "Delta", "Desc Delta", DateTime.UtcNow.AddDays(3), DateTime.UtcNow.AddDays(3).AddHours(1), 1),
        };

        foreach (var ev in events)
        {
            await repository.AddAsync(ev);
        }
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

        var query = await repository.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Event>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Single(result);
        Assert.Equal("Alpha", result.First().Title);
    }

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

        var query = await repository.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Event>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Title == "Beta");
        Assert.Contains(result, e => e.Title == "Gamma");
    }

    [Fact]
    public async Task SortByTitleDesc_ShouldReturnSortedEvents()
    {
        var repository = GetService<IRepository<Event>>();
        var filter = new EventFilterDto
        {
            SortBy = nameof(Event.Title),
            SortDesc = true,
            Page = 1,
            PageSize = 10
        };

        var query = await repository.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Event>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal("Gamma", result[0].Title);
        Assert.Equal("Delta", result[1].Title);
        Assert.Equal("Beta", result[2].Title);
        Assert.Equal("Alpha", result[3].Title);
    }

    [Fact]
    public async Task Pagination_ShouldReturnCorrectPage()
    {
        var repository = GetService<IRepository<Event>>();
        var filter = new EventFilterDto
        {
            Page = 2,
            PageSize = 2,
            SortBy = nameof(Event.Title),
            SortDesc = false
        };

        var query = await repository.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Event>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(2, result.Count);
        // Ожидаем, что на второй странице будут Delta и Gamma (сортировка по возрастанию названия)
        Assert.Equal("Delta", result[0].Title);
        Assert.Equal("Gamma", result[1].Title);
    }

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

        var query = await repository.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Event>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Single(result);
        Assert.Equal("Delta", result.First().Title);
    }

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

        var query = await repository.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Event>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Single(result);
        Assert.Equal("Beta", result.First().Title);
    }
}