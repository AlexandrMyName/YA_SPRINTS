using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Domain.Entities;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using SprintsASP_NetCore_API.Application.Abstractions;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для репозитория Booking.
/// </summary>
[Collection("DatabaseCollection")]
public class BookingRepositoryTests : TestBase
{
    public BookingRepositoryTests(DatabaseFixture fixture) : base(fixture) { }

    private async Task<Event> CreateTestEventAsync(int seats = 10)
    {
        var eventRepo = GetService<IRepository<Event>>();
        var ev = Event.Create(
            Guid.NewGuid(), "Test Event", "Description",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(2), seats);

        await eventRepo.AddAsync(ev);
        await eventRepo.SaveChangesAsync();
        return ev;
    }

    private async Task<User> CreateTestUserAsync(string login = "test_user")
    {
        var userRepo = GetService<IRepository<User>>();
        var user = User.Create(
            Guid.NewGuid(),
            login + "_" + Guid.NewGuid().ToString("N")[..6],
            "hash",
            UserRole.User);

        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();
        return user;
    }

    #region CRUD

    [Fact]
    public async Task AddAsync_ShouldAddBooking()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        var result = await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);

        var saved = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.NotNull(saved.Data);
        Assert.Equal(BookingStatus.Pending, saved.Data!.Status);
        Assert.Equal(user.Id, saved.Data.UserId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBookingStatus()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        booking.Confirm();

        var result = await bookingRepo.UpdateAsync(booking);
        await bookingRepo.SaveChangesAsync();

        Assert.True(result.IsSuccesfuly);

        var updated = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, updated.Data!.Status);
        Assert.NotNull(updated.Data.ProcessedAt);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnBookings()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Confirmed, DateTime.UtcNow);

        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        var all = await bookingRepo.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    #endregion

    #region Фильтрация

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByStatus()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Confirmed, DateTime.UtcNow);

        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        var paged = await bookingRepo.GetPagedAsync(
            predicate: b => b.Status == BookingStatus.Pending,
            page: 1,
            pageSize: 10);

        Assert.Equal(1, paged.TotalCount);

        var items = paged.Items.ToList();
        Assert.Single(items);
        Assert.Equal(BookingStatus.Pending, items[0].Status);
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterByUserId()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user1 = await CreateTestUserAsync("user1");
        var user2 = await CreateTestUserAsync("user2");

        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, user1.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, user2.Id, BookingStatus.Pending, DateTime.UtcNow);

        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        var paged = await bookingRepo.GetPagedAsync(
            predicate: b => b.UserId == user1.Id,
            page: 1,
            pageSize: 10);

        Assert.Equal(1, paged.TotalCount);
        Assert.Equal(user1.Id, paged.Items.First().UserId);
    }

    #endregion

    #region Оптимистичная блокировка

    [Fact]
    public async Task UpdateAsync_ShouldFailOnVersionConflict()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        var saved = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.NotNull(saved.Data);

        await DbContext.Database.ExecuteSqlRawAsync(
            "UPDATE bookings SET \"Status\" = 'Confirmed' WHERE \"Id\" = {0}", booking.Id);

        saved.Data!.Confirm();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await bookingRepo.UpdateAsync(saved.Data);
            await bookingRepo.SaveChangesAsync();
        });
    }

    #endregion

    #region Проверки существования

    [Fact]
    public async Task DeleteAsync_ShouldRemoveBooking()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        var deleteResult = await bookingRepo.DeleteAsync(booking.Id);
        await bookingRepo.SaveChangesAsync();

        Assert.True(deleteResult.IsSuccesfuly);

        var deleted = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.Null(deleted.Data);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailForNonExistentEntity()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var nonExistentId = Guid.NewGuid();

        var deleteResult = await bookingRepo.DeleteAsync(nonExistentId);

        Assert.False(deleteResult.IsSuccesfuly);
        Assert.False(bookingRepo.IsExisted(nonExistentId));
    }

    [Fact]
    public async Task IsExisted_ShouldReturnTrueForExisting()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        Assert.True(bookingRepo.IsExisted(booking.Id));
    }

    [Fact]
    public void IsExisted_ShouldReturnFalseForNonExisting()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        Assert.False(bookingRepo.IsExisted(Guid.NewGuid()));
    }

    #endregion

    #region Массовые операции

    [Fact]
    public async Task UpdateRangeAsync_ShouldUpdateMultipleBookings()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        b1.Confirm();
        b2.Confirm();

        var updateResult = await bookingRepo.UpdateRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        Assert.True(updateResult.IsSuccesfuly);

        var all = await bookingRepo.GetAllAsync();
        Assert.All(all, b => Assert.Equal(BookingStatus.Confirmed, b.Status));
    }

    #endregion

    #region Транзакции

    [Fact]
    public async Task BeginTransactionAsync_ShouldCommitSuccessfully()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await using var transaction = await bookingRepo.BeginTransactionAsync();
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();
        await transaction.CommitAsync();

        var saved = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.NotNull(saved.Data);
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldRollbackOnFailure()
    {
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var user = await CreateTestUserAsync();

        var booking = Booking.Create(
            Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending, DateTime.UtcNow);

        await using var transaction = await bookingRepo.BeginTransactionAsync();
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();
        await transaction.RollbackAsync();

        DbContext.ChangeTracker.Clear();

        using var scope = ServiceProvider.CreateScope();
        var freshRepo = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();
        var saved = await freshRepo.GetByIdAsync(booking.Id);

        Assert.Null(saved.Data);
    }

    #endregion
}