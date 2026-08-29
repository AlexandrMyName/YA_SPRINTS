using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;


namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для доменной логики
/// (Booking)
/// </summary>
public class BookingRepositoryTests : TestBase
{
    public BookingRepositoryTests()
    {
        // Конструктор пустой – репозитории будем получать внутри тестов
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearDatabaseAsync();
    }

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
        // Используем правильное имя колонки с кавычками (как в БД)
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
}