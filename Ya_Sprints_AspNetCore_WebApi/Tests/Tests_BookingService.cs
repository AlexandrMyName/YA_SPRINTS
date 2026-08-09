using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings; 
using Sprints_Project_ASP_NetCore_API.Services.DataServices;
using Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;
using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services.DataServices;
using SprintASP_NetCore_API.Services.Intercepts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Xunit;
using Moq;


namespace Tests
{

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

            var mapperConfig = new MapperConfiguration(cfg => {
                cfg.AddProfile<MappingEntityProfile>();
                cfg.AddProfile<MappingDtoProfile>();
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfig.CreateMapper());
             
            _serviceProvider = services.BuildServiceProvider();
        }


        public void Dispose() => _serviceProvider?.Dispose();


        private async Task<Event> CreateTestEvent(int totalSeats)
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
            var ev = Event.Create(Guid.NewGuid(), "Test Event", "Desc", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2), totalSeats);
            await repo.AddAsync(ev);
            await repo.SaveChangesAsync();
            return ev;
        }

        #region Tests

        [Fact]
        public async Task CreateBooking_DecreasesAvailableSeats_ByOne()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var ev = await CreateTestEvent(10);
            var initialSeats = ev.AvailableSeats;

            var result = await bookingService.CreateBookingAsync(ev.Id);

            Assert.True(result.IsSuccesfuly);
            var updated = await eventRepo.GetByIdAsync(ev.Id);
            Assert.Equal(initialSeats - 1, updated.Data.AvailableSeats);
        }

        [Fact]
        public async Task CreateMultipleBookings_UpToLimit_AllSucceed_AndHaveUniqueIds()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var ev = await CreateTestEvent(3);
            var ids = new HashSet<Guid>();

            for (int i = 0; i < 3; i++)
            {
                var result = await bookingService.CreateBookingAsync(ev.Id);
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

            var ev = await CreateTestEvent(1);
            await bookingService.CreateBookingAsync(ev.Id);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                bookingService.CreateBookingAsync(ev.Id));
        }

        [Fact]
        public async Task CreateBooking_ForNonExistentEvent_ThrowsKeyNotFoundException()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                bookingService.CreateBookingAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateBooking_WhenNoSeats_ThrowsNoAvailableSeatsException()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            var ev = await CreateTestEvent(1);
            await bookingService.CreateBookingAsync(ev.Id);

            await Assert.ThrowsAsync<NoAvailableSeatsException>(() =>
                bookingService.CreateBookingAsync(ev.Id));
        }

        [Fact]
        public async Task UpdateBooking_Confirm_ChangesStatusAndSetsProcessedAt()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var ev = await CreateTestEvent(5);
            var bookingResult = await bookingService.CreateBookingAsync(ev.Id);
            var bookingId = bookingResult.Data.Id;

            var updateDto = new BookingInfoDto
            {
                Id = bookingId,
                EventId = ev.Id,
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

            using var scope    = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var eventService   = scope.ServiceProvider.GetRequiredService<IEventService>();
            var eventRepo      = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var ev = await CreateTestEvent(3);
            await bookingService.CreateBookingAsync(ev.Id);
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

            var ev = await CreateTestEvent(1);
            var booking1 = await bookingService.CreateBookingAsync(ev.Id);
            Assert.True(booking1.IsSuccesfuly);

            await eventService.ReleaseSeatsAndUpdateAsync(ev.Id, 1);

            var booking2 = await bookingService.CreateBookingAsync(ev.Id);

            Assert.True(booking2.IsSuccesfuly);
            Assert.NotEqual(booking1.Data.Id, booking2.Data.Id);
        }

        [Fact]
        public async Task ConcurrentBookings_20Requests_5Seats_Exactly5Success_15Exceptions_AvailableSeatsZero()
        {

            using var scope = _serviceProvider.CreateScope();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
            var ev = await CreateTestEvent(5);
            var successCount = 0;
            var exceptionCount = 0;

            var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
            {
                using var innerScope = _serviceProvider.CreateScope();
                var bookingService = innerScope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    var result = await bookingService.CreateBookingAsync(ev.Id);
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
            var ev = await CreateTestEvent(10);
            var ids = new ConcurrentBag<Guid>();

            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
            {
                using var innerScope = _serviceProvider.CreateScope();
                var bookingService = innerScope.ServiceProvider.GetRequiredService<IBookingService>();
                var result = await bookingService.CreateBookingAsync(ev.Id);
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

        [Fact]
        public async Task GetBookingByIdAsync_WithValidId_ReturnsCorrectBooking()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var ev = await CreateTestEvent(5);
            var bookingResult = await bookingService.CreateBookingAsync(ev.Id);
            var bookingId = bookingResult.Data.Id;

            var result = await bookingService.GetBookingByIdAsync(bookingId);

            Assert.True(result.IsSuccesfuly);
            Assert.Equal(bookingId, result.Data.Id);
            Assert.Equal(BookingStatus.Pending, result.Data.Status);
        }

        [Fact]
        public async Task GetBookingByIdAsync_AfterStatusChange_ReflectsUpdatedStatus()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
            var ev = await CreateTestEvent(5);
            var bookingResult = await bookingService.CreateBookingAsync(ev.Id);
            var bookingId = bookingResult.Data.Id;

            var updateDto = new BookingInfoDto
            {
                Id = bookingId,
                EventId = ev.Id,
                Status = BookingStatus.Confirmed,
                ProcessedAt = DateTime.UtcNow
            };
            await bookingService.UpdateBookingAsync(updateDto);

            var after = await bookingService.GetBookingByIdAsync(bookingId);

            Assert.Equal(BookingStatus.Confirmed, after.Data.Status);
            Assert.NotNull(after.Data.ProcessedAt);
        }

        [Fact]
        public async Task CreateBookingAsync_WithNonExistentEvent_ReturnsFail()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                bookingService.CreateBookingAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetBookingByIdAsync_WithNonExistentId_ReturnsFail()
        {

            using var scope = _serviceProvider.CreateScope();
            var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                bookingService.GetBookingByIdAsync(Guid.NewGuid()));
        }

        #endregion
    }
}