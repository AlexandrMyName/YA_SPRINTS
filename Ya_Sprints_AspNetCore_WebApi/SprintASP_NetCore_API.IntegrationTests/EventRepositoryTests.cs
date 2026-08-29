using Microsoft.EntityFrameworkCore;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для доменной логики
/// (Event)
/// </summary>
[Collection("DatabaseCollection")]
public class EventRepositoryTests : TestBase
{
    public EventRepositoryTests(DatabaseFixture fixture) : base(fixture) { }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearDatabaseAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldAddEvent()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(
            Guid.NewGuid(),
            "Test Event",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            10
        );

        var result = await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);
        var saved = await repository.GetByIdAsync(ev.Id);
        Assert.NotNull(saved.Data);
        Assert.Equal("Test Event", saved.Data.Title);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEvent()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "Old Title", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        ev.Title = "New Title";
        var result = await repository.UpdateAsync(ev);
        await repository.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);
        var updated = await repository.GetByIdAsync(ev.Id);
        Assert.Equal("New Title", updated.Data.Title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEvent()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "ToDelete", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        var result = await repository.DeleteAsync(ev.Id);
        await repository.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);
        var deleted = await repository.GetByIdAsync(ev.Id);
        Assert.False(deleted.IsSuccesfuly);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEvents()
    {
        var repository = GetService<IRepository<Event>>();
        var ev1 = Event.Create(Guid.NewGuid(), "Event1", "Desc1", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        var ev2 = Event.Create(Guid.NewGuid(), "Event2", "Desc2", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);
        await repository.AddRangeAsync(new[] { ev1, ev2 });
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task GetQueryable_ShouldApplyFilterAndPagination()
    {
        var repository = GetService<IRepository<Event>>();
        for (int i = 1; i <= 10; i++)
        {
            // формат с ведущим нулём для правильной сортировки по строке
            var ev = Event.Create(Guid.NewGuid(), $"Event{i:D2}", $"Desc{i}", DateTime.UtcNow, DateTime.UtcNow.AddHours(i), i * 2);
            await repository.AddAsync(ev);
        }
        await repository.SaveChangesAsync();

        var query = await repository.GetQueryAsync();
        var filtered = query.OrderBy(e => e.Title).Skip(2).Take(5).ToList();

        Assert.Equal(5, filtered.Count);
        // Теперь ожидаем "Event03", потому что сортировка по строке даёт Event01, Event02, Event03, ...
        Assert.Equal("Event03", filtered.First().Title);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailOnVersionConflict()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "VersionTest", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();
         
        var saved = await repository.GetByIdAsync(ev.Id);

        // Имитация изменения строки другим пользователем (прямой SQL-запрос) 
        await DbContext.Database.ExecuteSqlRawAsync(
            "UPDATE events SET \"Title\" = 'Changed by other' WHERE \"Id\" = {0}", ev.Id);

        // Теперь пытаемся обновить с использованием нашего объекта (который имеет старую версию, но xmin уже изменён)
        saved.Data.Title = "New Title";

        // При сохранении должно выброситься DbUpdateConcurrencyException
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            var result = await repository.UpdateAsync(saved.Data);
            await repository.SaveChangesAsync();
        });
    }
}