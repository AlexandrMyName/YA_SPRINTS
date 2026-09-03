using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для репозитория Event
/// </summary>
[Collection("DatabaseCollection")]
public class EventRepositoryTests : TestBase
{
    public EventRepositoryTests(DatabaseFixture fixture) : base(fixture) { }
    

    #region Существующие тесты (без изменений)

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
            var ev = Event.Create(Guid.NewGuid(), $"Event{i:D2}", $"Desc{i}", DateTime.UtcNow, DateTime.UtcNow.AddHours(i), i * 2);
            await repository.AddAsync(ev);
        }
        await repository.SaveChangesAsync();

        var query = await repository.GetQueryAsync();
        var filtered = query.OrderBy(e => e.Title).Skip(2).Take(5).ToList();

        Assert.Equal(5, filtered.Count);
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

        saved.Data.Title = "New Title";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            var result = await repository.UpdateAsync(saved.Data);
            await repository.SaveChangesAsync();
        });
    }

    #endregion
 
    [Fact]
    public async Task DeleteAsync_ShouldFailForNonExistentEntity()
    {
        var repository = GetService<IRepository<Event>>();
        var nonExistentId = Guid.NewGuid();

        var deleteResult = await repository.DeleteAsync(nonExistentId);

        Assert.False(deleteResult.IsSuccesfuly);
        var exists = repository.IsExisted(nonExistentId);
        Assert.False(exists);
    }

    [Fact]
    public void IsExisted_ShouldReturnTrueForExisting()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "Exists", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        repository.AddAsync(ev).Wait();
        repository.SaveChangesAsync().Wait();

        var exists = repository.IsExisted(ev.Id);

        Assert.True(exists);
    }

    [Fact]
    public void IsExisted_ShouldReturnFalseForNonExisting()
    {
        var repository = GetService<IRepository<Event>>();
        var nonExistentId = Guid.NewGuid();

        var exists = repository.IsExisted(nonExistentId);

        Assert.False(exists);
    }

    [Fact]
    public void IsExistedByTitle_ShouldReturnTrueForExistingTitle()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "UniqueTitle", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        repository.AddAsync(ev).Wait();
        repository.SaveChangesAsync().Wait();

        var exists = repository.IsExistedByTitle("UniqueTitle");

        Assert.True(exists);
    }

    [Fact]
    public void IsExistedByTitle_ShouldReturnFalseForNonExistingTitle()
    {
        var repository = GetService<IRepository<Event>>();

        var exists = repository.IsExistedByTitle("NonExistingTitle");

        Assert.False(exists);
    }

    [Fact]
    public async Task UpdateRangeAsync_ShouldUpdateMultipleEvents()
    {
        var repository = GetService<IRepository<Event>>();
        var ev1 = Event.Create(Guid.NewGuid(), "Event1", "Desc1", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        var ev2 = Event.Create(Guid.NewGuid(), "Event2", "Desc2", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);
        await repository.AddRangeAsync(new[] { ev1, ev2 });
        await repository.SaveChangesAsync();

        // Обновляем
        ev1.Title = "Updated Event1";
        ev2.Title = "Updated Event2";
        var updateResult = await repository.UpdateRangeAsync(new[] { ev1, ev2 });
        await repository.SaveChangesAsync();

        Assert.True(updateResult.IsSuccesfuly);
        var all = await repository.GetAllAsync();
        Assert.Contains(all, e => e.Title == "Updated Event1");
        Assert.Contains(all, e => e.Title == "Updated Event2");
    }

    [Fact]
    public async Task UpdateBatchAsync_ShouldUpdateMultipleEventsWithoutLoading()
    {
        var repository = GetService<IRepository<Event>>();
        var ev1 = Event.Create(Guid.NewGuid(), "Batch1", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        var ev2 = Event.Create(Guid.NewGuid(), "Batch2", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);
        await repository.AddRangeAsync(new[] { ev1, ev2 });
        await repository.SaveChangesAsync();

        // Обновляем AvailableSeats у всех событий, у которых Title начинается с "Batch"
        var updatedCount = await repository.UpdateBatchAsync(
            e => e.Title.StartsWith("Batch"),
            setter => setter.SetProperty(e => e.AvailableSeats, e => e.AvailableSeats + 1)
        );

        Assert.Equal(2, updatedCount);

        // Проверяем через свежий scope
        using var scope = ServiceProvider.CreateScope();
        var freshRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
        var all = await freshRepo.GetAllAsync();
        Assert.All(all, e => Assert.Equal(e.Title == "Batch1" || e.Title == "Batch2", e.AvailableSeats == 6 || e.AvailableSeats == 11));
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldCommitSuccessfully()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "TransactionTest", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await using var transaction = await repository.BeginTransactionAsync();
        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();
        await transaction.CommitAsync();

        var saved = await repository.GetByIdAsync(ev.Id);
        Assert.NotNull(saved.Data);
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldRollbackOnFailure()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "RollbackTest", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await using var transaction = await repository.BeginTransactionAsync();
        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Создаём новый scope, чтобы получить свежий контекст без кэша
        using var scope = ServiceProvider.CreateScope();
        var freshRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
        var saved = await freshRepo.GetByIdAsync(ev.Id);
        Assert.Null(saved.Data);
    }
     
}