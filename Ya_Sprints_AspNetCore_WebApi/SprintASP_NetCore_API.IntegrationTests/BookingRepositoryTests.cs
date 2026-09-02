using SprintASP_NetCore_API.IntegrationTests.Fixture;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;


namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для доменной логики
/// (Booking)
/// </summary>
[Collection("DatabaseCollection")]
public class BookingRepositoryTests : TestBase
{

    public BookingRepositoryTests(DatabaseFixture fixture) : base(fixture) { }

    
    private async Task<Event> CreateTestEventAsync()
    {
        var eventRepo = GetService<IRepository<Event>>();
        var ev = Event.Create(
            Guid.NewGuid(),
            "Test Event",
            "Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(2),
            10
        );
        await eventRepo.AddAsync(ev);
        await eventRepo.SaveChangesAsync();
        return ev;
    }

    [Fact]
    public async Task AddAsync_ShouldAddBooking()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        // Act
        var result = await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Assert
        Assert.True(result.IsSuccesfuly);
        var saved = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.NotNull(saved.Data);
        Assert.Equal(BookingStatus.Pending, saved.Data.Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBookingStatus()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Act
        booking.Confirm();
        booking.ProcessedAt = DateTime.UtcNow;
        var result = await bookingRepo.UpdateAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Assert
        Assert.True(result.IsSuccesfuly);
        var updated = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, updated.Data.Status);
        Assert.NotNull(updated.Data.ProcessedAt);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnBookings()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Confirmed, DateTime.UtcNow);
        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        // Act
        var all = await bookingRepo.GetAllAsync();

        // Assert
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task GetQueryable_ShouldFilterByStatus()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Confirmed, DateTime.UtcNow);
        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        // Act
        var query = await bookingRepo.GetQueryAsync();
        var pending = query.Where(b => b.Status == BookingStatus.Pending).ToList();

        // Assert
        Assert.Single(pending);
        Assert.Equal(BookingStatus.Pending, pending.First().Status);
    }

    [Fact]
    public async Task UpdateAsync_ShouldFailOnVersionConflict()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Получаем объект для обновления (отслеживается)
        var saved = await bookingRepo.GetByIdAsync(booking.Id);

        // Симулируем изменение строки другим пользователем (прямой SQL) 
        await DbContext.Database.ExecuteSqlRawAsync(
            "UPDATE bookings SET \"Status\" = 'Confirmed' WHERE \"Id\" = {0}", booking.Id);

        // Теперь пытаемся обновить с использованием нашего объекта (xmin уже изменён)
        saved.Data.Confirm();
        saved.Data.ProcessedAt = DateTime.UtcNow;

        // При сохранении должно выброситься DbUpdateConcurrencyException
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            var result = await bookingRepo.UpdateAsync(saved.Data);
            await bookingRepo.SaveChangesAsync();
        });
    }


    // ____ 

    [Fact]
    public async Task DeleteAsync_ShouldRemoveBooking()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();

        // Act
        var deleteResult = await bookingRepo.DeleteAsync(booking.Id);
        await bookingRepo.SaveChangesAsync();

        // Assert
        Assert.True(deleteResult.IsSuccesfuly);
        var deleted = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.Null(deleted.Data);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailForNonExistentEntity()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var nonExistentId = Guid.NewGuid();

        // Act
        var deleteResult = await bookingRepo.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(deleteResult.IsSuccesfuly);
        // Проверяем, что запись действительно не существовала
        var exists = bookingRepo.IsExisted(nonExistentId);
        Assert.False(exists);
    }

    [Fact]
    public void IsExisted_ShouldReturnTrueForExisting()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = CreateTestEventAsync().Result;  
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );
        bookingRepo.AddAsync(booking).Wait();
        bookingRepo.SaveChangesAsync().Wait();

        // Act
        var exists = bookingRepo.IsExisted(booking.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public void IsExisted_ShouldReturnFalseForNonExisting()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = bookingRepo.IsExisted(nonExistentId);

        // Assert
        Assert.False(exists);
    }
    
    [Fact]
    public async Task UpdateRangeAsync_ShouldUpdateMultipleBookings()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow);
        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        // Act
        b1.Confirm();
        b1.ProcessedAt = DateTime.UtcNow;
        b2.Confirm();
        b2.ProcessedAt = DateTime.UtcNow;
        var updateResult = await bookingRepo.UpdateRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        // Assert
        Assert.True(updateResult.IsSuccesfuly);
        var all = await bookingRepo.GetAllAsync();
        Assert.All(all, b => Assert.Equal(BookingStatus.Confirmed, b.Status));
    }

    [Fact]
    public async Task UpdateBatchAsync_ShouldUpdateMultipleBookingsWithoutLoading()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var b1 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow);
        var b2 = Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow);
        await bookingRepo.AddRangeAsync(new[] { b1, b2 });
        await bookingRepo.SaveChangesAsync();

        // Act
        var updatedCount = await bookingRepo.UpdateBatchAsync(
            b => b.EventId == ev.Id,
            setter => setter.SetProperty(b => b.Status,  BookingStatus.Confirmed)  
        );

        // Assert
        Assert.Equal(2, updatedCount);

        // Проверяем через свежий scope
        using var scope = ServiceProvider.CreateScope();
        var freshRepo = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();
        var all = await freshRepo.GetAllAsync();
        Assert.All(all, b => Assert.Equal(BookingStatus.Confirmed, b.Status));
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldCommitSuccessfully()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        // Act
        await using var transaction = await bookingRepo.BeginTransactionAsync();
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();
        await transaction.CommitAsync();

        // Assert
        var saved = await bookingRepo.GetByIdAsync(booking.Id);
        Assert.NotNull(saved.Data);
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldRollbackOnFailure()
    {
        // Arrange
        var bookingRepo = GetService<IRepository<Booking>>();
        var ev = await CreateTestEventAsync();
        var booking = Booking.Create(
            Guid.NewGuid(),
            ev.Id,
            BookingStatus.Pending,
            DateTime.UtcNow
        );

        // Act
        await using var transaction = await bookingRepo.BeginTransactionAsync();
        await bookingRepo.AddAsync(booking);
        await bookingRepo.SaveChangesAsync();
        await transaction.RollbackAsync();
         
        DbContext.ChangeTracker.Clear();

        // Создаём новый scope, чтобы получить свежий репозиторий с новым контекстом
        using var scope = ServiceProvider.CreateScope();
        var freshRepo = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();
        var saved = await freshRepo.GetByIdAsync(booking.Id);
        Assert.Null(saved.Data);
    }
}