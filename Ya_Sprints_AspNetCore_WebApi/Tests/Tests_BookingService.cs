using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using SprintASP_NetCore_API.Application.UseCases.DataServices;
using SprintsASP_NetCore_API.Infrastructure.Concurrency;
using SprintASP_NetCore_API.Infrastructure.Repositories;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Application.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection; 
using SprintsASP_NetCore_API.Domain.Exceptions;
using SprintASP_NetCore_API.Domain.Entities;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Xunit;
using Moq;


namespace Tests;


public class BookingServiceTests : IDisposable
{

    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;


    public BookingServiceTests()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventService, EventsService>();
        services.AddSingleton<IInterceptLockings, InterceptLockings>();

        services.AddSingleton<ILogger<BookingService>>(sp => Mock.Of<ILogger<BookingService>>());
        services.AddSingleton<ILogger<EventsService>>(sp => Mock.Of<ILogger<EventsService>>());

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingEntityProfile>();
            cfg.AddProfile<MappingDtoProfile>();
        }, NullLoggerFactory.Instance);
        services.AddSingleton<IMapper>(mapperConfig.CreateMapper());

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider?.Dispose();

    // Хелперы  

    private async Task<Event> CreateTestEventAsync(
        int totalSeats,
        DateTime? startAt = null,
        DateTime? endAt = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        var end = endAt ?? start.AddDays(1);

        var ev = Event.Create(Guid.NewGuid(), "Test Event", "Desc", start, end, totalSeats);
        await repo.AddAsync(ev);
        await repo.SaveChangesAsync();
        return ev;
    }

    private async Task<User> CreateTestUserAsync(string loginPrefix = "user")
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();

        var login = loginPrefix + "_" + Guid.NewGuid().ToString("N")[..6];
        var user = User.Create(Guid.NewGuid(), login, "hash", UserRole.User);

        await repo.AddAsync(user);
        await repo.SaveChangesAsync();
        return user;
    }

    #region CRUD и базовые сценарии

    [Fact]
    public async Task CreateBooking_DecreasesAvailableSeats_ByOne()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

        var ev = await CreateTestEventAsync(10);
        var user = await CreateTestUserAsync();
        var initialSeats = ev.AvailableSeats;

        var result = await bookingService.CreateBookingAsync(ev.Id, user.Id);

        Assert.True(result.IsSuccesfuly);
        var updated = await eventRepo.GetByIdAsync(ev.Id);
        Assert.Equal(initialSeats - 1, updated.Data.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_SavesUserId()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var user = await CreateTestUserAsync();

        var result = await bookingService.CreateBookingAsync(ev.Id, user.Id);

        Assert.True(result.IsSuccesfuly);
        Assert.Equal(user.Id, result.Data.UserId);
    }

    [Fact]
    public async Task CreateMultipleBookings_UpToLimit_AllSucceed_AndHaveUniqueIds()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

        var ev = await CreateTestEventAsync(3);
        var user = await CreateTestUserAsync();
        var ids = new HashSet<Guid>();

        for (int i = 0; i < 3; i++)
        {
            var result = await bookingService.CreateBookingAsync(ev.Id, user.Id);
            Assert.True(result.IsSuccesfuly);
            ids.Add(result.Data.Id);
        }

        Assert.Equal(3, ids.Count);
        var updated = await eventRepo.GetByIdAsync(ev.Id);
        Assert.Equal(0, updated.Data.AvailableSeats);
    }

    [Fact]
    public async Task CreateBooking_WhenSeatsExhausted_ThrowsNoAvailableSeatsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(1);
        var user = await CreateTestUserAsync();
        await bookingService.CreateBookingAsync(ev.Id, user.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
            bookingService.CreateBookingAsync(ev.Id, user.Id));
    }

    [Fact]
    public async Task CreateBooking_ForNonExistentEvent_ThrowsKeyNotFoundException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user = await CreateTestUserAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            bookingService.CreateBookingAsync(Guid.NewGuid(), user.Id));
    }

    [Fact]
    public async Task UpdateBooking_Confirm_ChangesStatusAndSetsProcessedAt()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var ev = await CreateTestEventAsync(5);
        var user = await CreateTestUserAsync();

        var bookingResult = await bookingService.CreateBookingAsync(ev.Id, user.Id);
        var bookingId = bookingResult.Data.Id;

        var updateDto = new BookingInfoDto
        {
            Id = bookingId,
            EventId = ev.Id,
            UserId = user.Id,
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow
        };

        var result = await bookingService.UpdateBookingAsync(updateDto);

        Assert.True(result.IsSuccesfuly);
        Assert.Equal(BookingStatus.Confirmed, result.Data.Status);
        Assert.NotNull(result.Data.ProcessedAt);
    }

    [Fact]
    public async Task RejectBooking_ReleasesSeat()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

        var ev = await CreateTestEventAsync(3);
        var user = await CreateTestUserAsync();

        await bookingService.CreateBookingAsync(ev.Id, user.Id);
        var initialAfterCreate = (await eventRepo.GetByIdAsync(ev.Id)).Data.AvailableSeats;
        Assert.Equal(2, initialAfterCreate);

        await eventService.ReleaseSeatsAndUpdateAsync(ev.Id, 1);

        var finalSeats = (await eventRepo.GetByIdAsync(ev.Id)).Data.AvailableSeats;
        Assert.Equal(3, finalSeats);
    }

    [Fact]
    public async Task AfterReject_CanCreateNewBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

        var ev = await CreateTestEventAsync(1);
        var user = await CreateTestUserAsync();

        var booking1 = await bookingService.CreateBookingAsync(ev.Id, user.Id);
        Assert.True(booking1.IsSuccesfuly);

        await eventService.ReleaseSeatsAndUpdateAsync(ev.Id, 1);

        var booking2 = await bookingService.CreateBookingAsync(ev.Id, user.Id);

        Assert.True(booking2.IsSuccesfuly);
        Assert.NotEqual(booking1.Data.Id, booking2.Data.Id);
    }

    #endregion

    #region НОВЫЕ БИЗНЕС-ПРАВИЛА

    [Fact]
    public async Task CreateBooking_ForPastEvent_ThrowsEventAlreadyStartedException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var pastStart = DateTime.UtcNow.AddDays(-2);
        var pastEnd = DateTime.UtcNow.AddDays(-1);

        var pastEvent = await CreateTestEventAsync(10, pastStart, pastEnd);
        var user = await CreateTestUserAsync();

        await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            bookingService.CreateBookingAsync(pastEvent.Id, user.Id));
    }

    [Fact]
    public async Task CreateBooking_ForEventStartingNow_ThrowsEventAlreadyStartedException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        // StartAt == UtcNow — «уже началось»
        var now = DateTime.UtcNow;
        var ev = await CreateTestEventAsync(10, now, now.AddHours(2));
        var user = await CreateTestUserAsync();

        await Assert.ThrowsAsync<EventAlreadyStartedException>(() =>
            bookingService.CreateBookingAsync(ev.Id, user.Id));
    }

    [Fact]
    public async Task CreateBooking_WhenUserReachesLimit_ThrowsActiveBookingsLimitExceededException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user = await CreateTestUserAsync("limit_user");

        // Создаём 11 разных событий
        var eventIds = new List<Guid>();
        for (int i = 0; i < 11; i++)
        {
            var ev = await CreateTestEventAsync(10);
            eventIds.Add(ev.Id);
        }

        // Первые 10 броней проходят
        for (int i = 0; i < 10; i++)
        {
            var result = await bookingService.CreateBookingAsync(eventIds[i], user.Id);
            Assert.True(result.IsSuccesfuly);
        }

        // 11-я — превышение лимита
        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(() =>
            bookingService.CreateBookingAsync(eventIds[10], user.Id));
    }

    [Fact]
    public async Task CreateBooking_DifferentUsersHaveIndependentLimits()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user1 = await CreateTestUserAsync("indep_user1");
        var user2 = await CreateTestUserAsync("indep_user2");

        // Оба пользователя набирают по 10 активных броней
        for (int i = 0; i < 10; i++)
        {
            var ev1 = await CreateTestEventAsync(10);
            var r1 = await bookingService.CreateBookingAsync(ev1.Id, user1.Id);
            Assert.True(r1.IsSuccesfuly);

            var ev2 = await CreateTestEventAsync(10);
            var r2 = await bookingService.CreateBookingAsync(ev2.Id, user2.Id);
            Assert.True(r2.IsSuccesfuly);
        }

        // Оба достигли лимита — оба получают исключение
        var newEv1 = await CreateTestEventAsync(10);
        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(() =>
            bookingService.CreateBookingAsync(newEv1.Id, user1.Id));

        var newEv2 = await CreateTestEventAsync(10);
        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(() =>
            bookingService.CreateBookingAsync(newEv2.Id, user2.Id));
    }

    [Fact]
    public async Task CreateBooking_AfterCancelling_UserCanBookAgain()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var user = await CreateTestUserAsync("cancel_user");

        // Заполняем лимит
        var eventIds = new List<Guid>();
        for (int i = 0; i < 10; i++)
        {
            var ev = await CreateTestEventAsync(10);
            eventIds.Add(ev.Id);
            var r = await bookingService.CreateBookingAsync(ev.Id, user.Id);
            Assert.True(r.IsSuccesfuly);
        }

        // 11-я падает по лимиту
        var overflowEvent = await CreateTestEventAsync(10);
        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(() =>
            bookingService.CreateBookingAsync(overflowEvent.Id, user.Id));

        // Отменяем одну бронь
        var bookings = await bookingService.GetPendingBookingsAsync();
        var ownBooking = bookings.First(b => b.UserId == user.Id);
        var cancel = await bookingService.CancelBookingAsync(ownBooking.Id, user.Id, isAdmin: false);
        Assert.True(cancel.IsSuccesfuly);

        // Теперь снова есть место в лимите
        var result = await bookingService.CreateBookingAsync(overflowEvent.Id, user.Id);
        Assert.True(result.IsSuccesfuly);
    }

    #endregion

    #region Отмена — права

    [Fact]
    public async Task CancelBooking_OwnBooking_Succeeds()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var user = await CreateTestUserAsync();

        var created = await bookingService.CreateBookingAsync(ev.Id, user.Id);
        var bookingId = created.Data.Id;

        var cancel = await bookingService.CancelBookingAsync(bookingId, user.Id, isAdmin: false);

        Assert.True(cancel.IsSuccesfuly);
        Assert.Equal(BookingStatus.Cancelled, cancel.Data.Status);
    }

    [Fact]
    public async Task CancelBooking_ForeignBooking_WithoutAdmin_ThrowsNoRightsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var owner = await CreateTestUserAsync("owner");
        var stranger = await CreateTestUserAsync("stranger");

        var created = await bookingService.CreateBookingAsync(ev.Id, owner.Id);
        var bookingId = created.Data.Id;

        await Assert.ThrowsAsync<NoRightsException>(() =>
            bookingService.CancelBookingAsync(bookingId, stranger.Id, isAdmin: false));
    }

    [Fact]
    public async Task CancelBooking_ForeignBooking_AsAdmin_Succeeds()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var owner = await CreateTestUserAsync("owner2");
        var admin = await CreateTestUserAsync("admin2");

        var created = await bookingService.CreateBookingAsync(ev.Id, owner.Id);
        var bookingId = created.Data.Id;

        var cancel = await bookingService.CancelBookingAsync(bookingId, admin.Id, isAdmin: true);

        Assert.True(cancel.IsSuccesfuly);
        Assert.Equal(BookingStatus.Cancelled, cancel.Data.Status);
    }

    [Fact]
    public async Task CancelBooking_Twice_ThrowsBookingAlreadyCancelledException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var user = await CreateTestUserAsync();

        var created = await bookingService.CreateBookingAsync(ev.Id, user.Id);
        var bookingId = created.Data.Id;

        await bookingService.CancelBookingAsync(bookingId, user.Id, isAdmin: false);

        await Assert.ThrowsAsync<BookingAlreadyCancelledException>(() =>
            bookingService.CancelBookingAsync(bookingId, user.Id, isAdmin: false));
    }

    [Fact]
    public async Task CancelBooking_ReleasesSeat()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

        var ev = await CreateTestEventAsync(3);
        var user = await CreateTestUserAsync();

        var created = await bookingService.CreateBookingAsync(ev.Id, user.Id);

        var seatsAfterBooking = (await eventRepo.GetByIdAsync(ev.Id)).Data.AvailableSeats;
        Assert.Equal(2, seatsAfterBooking);

        await bookingService.CancelBookingAsync(created.Data.Id, user.Id, isAdmin: false);

        var seatsAfterCancel = (await eventRepo.GetByIdAsync(ev.Id)).Data.AvailableSeats;
        Assert.Equal(3, seatsAfterCancel);
    }

    #endregion

    #region Чтение — права

    [Fact]
    public async Task GetBookingByIdAsync_WithValidId_ReturnsCorrectBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var user = await CreateTestUserAsync();

        var bookingResult = await bookingService.CreateBookingAsync(ev.Id, user.Id);
        var bookingId = bookingResult.Data.Id;

        var result = await bookingService.GetBookingByIdAsync(bookingId, user.Id, isAdmin: false);

        Assert.True(result.IsSuccesfuly);
        Assert.Equal(bookingId, result.Data.Id);
        Assert.Equal(BookingStatus.Pending, result.Data.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AsAdmin_CanReadForeignBooking()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var owner = await CreateTestUserAsync("read_owner");
        var admin = await CreateTestUserAsync("read_admin");

        var bookingResult = await bookingService.CreateBookingAsync(ev.Id, owner.Id);
        var bookingId = bookingResult.Data.Id;

        var result = await bookingService.GetBookingByIdAsync(bookingId, admin.Id, isAdmin: true);

        Assert.True(result.IsSuccesfuly);
        Assert.Equal(owner.Id, result.Data.UserId);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ForeignBooking_WithoutAdmin_ThrowsNoRightsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var owner = await CreateTestUserAsync("read_owner2");
        var stranger = await CreateTestUserAsync("read_stranger");

        var bookingResult = await bookingService.CreateBookingAsync(ev.Id, owner.Id);

        await Assert.ThrowsAsync<NoRightsException>(() =>
            bookingService.GetBookingByIdAsync(bookingResult.Data.Id, stranger.Id, isAdmin: false));
    }

    [Fact]
    public async Task GetBookingByIdAsync_WithNonExistentId_ThrowsKeyNotFoundException()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var user = await CreateTestUserAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            bookingService.GetBookingByIdAsync(Guid.NewGuid(), user.Id, isAdmin: false));
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterStatusChange_ReflectsUpdatedStatus()
    {
        using var scope = _serviceProvider.CreateScope();
        var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var ev = await CreateTestEventAsync(5);
        var user = await CreateTestUserAsync();

        var bookingResult = await bookingService.CreateBookingAsync(ev.Id, user.Id);
        var bookingId = bookingResult.Data.Id;

        var updateDto = new BookingInfoDto
        {
            Id = bookingId,
            EventId = ev.Id,
            UserId = user.Id,
            Status = BookingStatus.Confirmed,
            ProcessedAt = DateTime.UtcNow
        };
        await bookingService.UpdateBookingAsync(updateDto);

        var after = await bookingService.GetBookingByIdAsync(bookingId, user.Id, isAdmin: false);

        Assert.Equal(BookingStatus.Confirmed, after.Data.Status);
        Assert.NotNull(after.Data.ProcessedAt);
    }

    #endregion

    #region Конкурентность

    [Fact]
    public async Task ConcurrentBookings_20Requests_5Seats_Exactly5Success_15Exceptions_AvailableSeatsZero()
    {
        using var scope = _serviceProvider.CreateScope();
        var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
        var ev = await CreateTestEventAsync(5);

        var successCount = 0;
        var exceptionCount = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            using var innerScope = _serviceProvider.CreateScope();
            var bookingService = innerScope.ServiceProvider.GetRequiredService<IBookingService>();

            // Отдельный пользователь на каждый запрос — чтобы не упереться в лимит 10
            var user = await CreateTestUserAsync("concurrent");

            try
            {
                var result = await bookingService.CreateBookingAsync(ev.Id, user.Id);
                if (result.IsSuccesfuly) Interlocked.Increment(ref successCount);
            }
            catch (NoAvailableSeatsException)
            {
                Interlocked.Increment(ref exceptionCount);
            }
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(5, successCount);
        Assert.Equal(15, exceptionCount);
        var final = await eventRepo.GetByIdAsync(ev.Id);
        Assert.Equal(0, final.Data.AvailableSeats);
    }

    [Fact]
    public async Task ConcurrentBookings_10Requests_10Seats_AllUniqueIds()
    {
        using var scope = _serviceProvider.CreateScope();
        var ev = await CreateTestEventAsync(10);
        var ids = new ConcurrentBag<Guid>();

        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            using var innerScope = _serviceProvider.CreateScope();
            var bookingService = innerScope.ServiceProvider.GetRequiredService<IBookingService>();
            var user = await CreateTestUserAsync("concurrent10");

            var result = await bookingService.CreateBookingAsync(ev.Id, user.Id);
            if (result.IsSuccesfuly) ids.Add(result.Data.Id);
        }));

        await Task.WhenAll(tasks);

        var distinctIds = ids.Distinct().Count();
        Assert.Equal(10, distinctIds);

        using var finalScope = _serviceProvider.CreateScope();
        var eventRepo = finalScope.ServiceProvider.GetRequiredService<IRepository<Event>>();
        var final = await eventRepo.GetByIdAsync(ev.Id);
        Assert.Equal(0, final.Data.AvailableSeats);
    }

    #endregion
}