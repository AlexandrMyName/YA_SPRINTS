using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Domain.Entities;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using SprintsASP_NetCore_API.Application.Abstractions;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для репозитория Event.
/// </summary>
[Collection("DatabaseCollection")]
public class EventRepositoryTests : TestBase
{
    public EventRepositoryTests(DatabaseFixture fixture) : base(fixture) { }

    #region CRUD

    [Fact]
    public async Task AddAsync_ShouldAddEvent()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(
            Guid.NewGuid(), "Test Event", "Description",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);

        var result = await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);

        var saved = await repository.GetByIdAsync(ev.Id);
        Assert.NotNull(saved.Data);
        Assert.Equal("Test Event", saved.Data!.Title);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEvent()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "Old Title", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        ev.Title = "New Title";
        var result = await repository.UpdateAsync(ev);
        await repository.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);

        var updated = await repository.GetByIdAsync(ev.Id);
        Assert.Equal("New Title", updated.Data!.Title);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEvent()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "ToDelete", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

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
        var ev1 = Event.Create(Guid.NewGuid(), "Event1", "Desc1",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        var ev2 = Event.Create(Guid.NewGuid(), "Event2", "Desc2",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);

        await repository.AddRangeAsync(new[] { ev1, ev2 });
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    #endregion

    #region Пагинация

    [Fact]
    public async Task GetPagedAsync_ShouldApplyPagination()
    {
        var repository = GetService<IRepository<Event>>();

        for (int i = 1; i <= 10; i++)
        {
            var ev = Event.Create(Guid.NewGuid(), $"Event{i:D2}", $"Desc{i}",
                DateTime.UtcNow, DateTime.UtcNow.AddHours(i), i * 2);
            await repository.AddAsync(ev);
        }
        await repository.SaveChangesAsync();

        // Первая страница: 5 элементов
        var page1 = await repository.GetPagedAsync(
            predicate: null, page: 1, pageSize: 5);

        Assert.Equal(10, page1.TotalCount);

        var items1 = page1.Items.ToList();
        Assert.Equal(5, items1.Count);

        // Вторая страница: 5 элементов
        var page2 = await repository.GetPagedAsync(
            predicate: null, page: 2, pageSize: 5);

        Assert.Equal(10, page2.TotalCount);

        var items2 = page2.Items.ToList();
        Assert.Equal(5, items2.Count);

        // Проверяем, что страницы не пересекаются
        var ids1 = items1.Select(e => e.Id).ToHashSet();
        var ids2 = items2.Select(e => e.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));
    }

    [Fact]
    public async Task GetPagedAsync_WithPredicate_ShouldFilterAndPaginate()
    {
        var repository = GetService<IRepository<Event>>();

        for (int i = 1; i <= 10; i++)
        {
            var ev = Event.Create(Guid.NewGuid(), $"Event{i:D2}", $"Desc{i}",
                DateTime.UtcNow, DateTime.UtcNow.AddHours(i), i * 2);
            await repository.AddAsync(ev);
        }
        await repository.SaveChangesAsync();

        // Фильтр: только те, у кого TotalSeats > 10 (Event06..Event10)
        var paged = await repository.GetPagedAsync(
            predicate: e => e.TotalSeats > 10, page: 1, pageSize: 100);

        Assert.Equal(5, paged.TotalCount);
        Assert.All(paged.Items, e => Assert.True(e.TotalSeats > 10));
    }

    #endregion

    #region Оптимистичная блокировка

    [Fact]
    public async Task UpdateAsync_ShouldFailOnVersionConflict()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "VersionTest", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        var saved = await repository.GetByIdAsync(ev.Id);
        Assert.NotNull(saved.Data);

        // Имитация изменения строки другим пользователем
        await DbContext.Database.ExecuteSqlRawAsync(
            "UPDATE events SET \"Title\" = 'Changed by other' WHERE \"Id\" = {0}", ev.Id);

        saved.Data!.Title = "New Title";

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await repository.UpdateAsync(saved.Data);
            await repository.SaveChangesAsync();
        });
    }

    #endregion

    #region Проверки существования

    [Fact]
    public async Task DeleteAsync_ShouldFailForNonExistentEntity()
    {
        var repository = GetService<IRepository<Event>>();
        var nonExistentId = Guid.NewGuid();

        var deleteResult = await repository.DeleteAsync(nonExistentId);

        Assert.False(deleteResult.IsSuccesfuly);
        Assert.False(repository.IsExisted(nonExistentId));
    }

    [Fact]
    public async Task IsExisted_ShouldReturnTrueForExisting()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "Exists", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        Assert.True(repository.IsExisted(ev.Id));
    }

    [Fact]
    public void IsExisted_ShouldReturnFalseForNonExisting()
    {
        var repository = GetService<IRepository<Event>>();
        Assert.False(repository.IsExisted(Guid.NewGuid()));
    }

    [Fact]
    public async Task IsExistedByTitle_ShouldReturnTrueForExistingTitle()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "UniqueTitle", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();

        Assert.True(repository.IsExistedByTitle("UniqueTitle"));
    }

    [Fact]
    public void IsExistedByTitle_ShouldReturnFalseForNonExistingTitle()
    {
        var repository = GetService<IRepository<Event>>();
        Assert.False(repository.IsExistedByTitle("NonExistingTitle"));
    }

    #endregion

    #region Массовые операции

    [Fact]
    public async Task UpdateRangeAsync_ShouldUpdateMultipleEvents()
    {
        var repository = GetService<IRepository<Event>>();
        var ev1 = Event.Create(Guid.NewGuid(), "Event1", "Desc1",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        var ev2 = Event.Create(Guid.NewGuid(), "Event2", "Desc2",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);

        await repository.AddRangeAsync(new[] { ev1, ev2 });
        await repository.SaveChangesAsync();

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
    public async Task AddRangeAsync_ShouldAddMultipleEvents()
    {
        var repository = GetService<IRepository<Event>>();
        var ev1 = Event.Create(Guid.NewGuid(), "Bulk1", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
        var ev2 = Event.Create(Guid.NewGuid(), "Bulk2", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);
        var ev3 = Event.Create(Guid.NewGuid(), "Bulk3", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(3), 15);

        var addResult = await repository.AddRangeAsync(new[] { ev1, ev2, ev3 });
        await repository.SaveChangesAsync();

        Assert.True(addResult.IsSuccesfuly);

        var all = await repository.GetAllAsync();
        Assert.Equal(3, all.Count());
    }

    #endregion

    #region Транзакции

    [Fact]
    public async Task BeginTransactionAsync_ShouldCommitSuccessfully()
    {
        var repository = GetService<IRepository<Event>>();
        var ev = Event.Create(Guid.NewGuid(), "TransactionTest", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

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
        var ev = Event.Create(Guid.NewGuid(), "RollbackTest", "Desc",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);

        await using var transaction = await repository.BeginTransactionAsync();
        await repository.AddAsync(ev);
        await repository.SaveChangesAsync();
        await transaction.RollbackAsync();

        // Свежий scope без кэша
        using var scope = ServiceProvider.CreateScope();
        var freshRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
        var saved = await freshRepo.GetByIdAsync(ev.Id);

        Assert.True(saved.Data == null || !saved.IsSuccesfuly);
    }

    #endregion
}