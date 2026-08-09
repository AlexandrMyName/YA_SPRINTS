using Sprints_Project_ASP_NetCore_API.Services.DataServices;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;
using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services.Intercepts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SprintASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Xunit;
using Moq;


namespace Tests
{

    public class Tests_EventsService_Integration : IDisposable
    {

        private readonly ServiceProvider _serviceProvider;
        private readonly string _dbName;


        public Tests_EventsService_Integration()
        {

            _dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();

            // InMemory DbContext
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));

            // Репозитории
            services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));

            // Сервисы
            services.AddScoped<IEventService, EventsService>();
            services.AddSingleton<IInterceptLockings, InterceptLockings>();

            // Логгеры (моки)
            services.AddSingleton<ILogger<EventsService>>(sp => Mock.Of<ILogger<EventsService>>());

            // AutoMapper
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingEntityProfile>();
                cfg.AddProfile<MappingDtoProfile>();
            }, NullLoggerFactory.Instance);
            services.AddSingleton<IMapper>(mapperConfig.CreateMapper());

            _serviceProvider = services.BuildServiceProvider();
        }


        public void Dispose() => _serviceProvider?.Dispose();


        private IEventService GetService() => _serviceProvider.GetRequiredService<IEventService>();
        private IRepository<Event> GetRepository() => _serviceProvider.GetRequiredService<IRepository<Event>>();


        #region Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllEvents()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var testEvent = Event.Create(Guid.NewGuid(), "Test Event", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
            await repo.AddAsync(testEvent);
            await repo.SaveChangesAsync();

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Single(list);
            Assert.Equal(testEvent.Title, list.First().Title);
        }

        [Fact]
        public async Task GetAllAsync_WhenRepositoryEmpty_ShouldReturnEmptyList()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnEvent()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var testEvent = Event.Create(Guid.NewGuid(), "Test", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
            await repo.AddAsync(testEvent);
            await repo.SaveChangesAsync();

            var result = await service.GetByIdAsync(testEvent.Id);

            Assert.NotNull(result);
            Assert.True(result.IsSuccesfuly);
            Assert.Equal(testEvent.Title, result.Data.Title);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnFail()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var result = await service.GetByIdAsync(Guid.NewGuid());

            Assert.NotNull(result);
            Assert.False(result.IsSuccesfuly);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task AddAsync_WithValidDto_ShouldAddEvent()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var dto = new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = "New Event",
                Description = "Desc",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 10
            };

            var result = await service.AddAsync(dto);

            Assert.NotNull(result);
            Assert.True(result.IsSuccesfuly);
            var added = await repo.GetByIdAsync(dto.Id);
            Assert.NotNull(added.Data);
            Assert.Equal(dto.Title, added.Data.Title);
        }

        [Fact]
        public async Task AddAsync_WhenAddFails_ShouldReturnFail()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var id = Guid.NewGuid();
            var existing = Event.Create(id, "Existing", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
            await repo.AddAsync(existing);
            await repo.SaveChangesAsync();

            var dto = new EventInfoDto
            {
                Id = id,
                Title = "Duplicate",
                Description = "Desc",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
                TotalSeats = 10
            };

            var result = await service.AddAsync(dto);

            Assert.NotNull(result);
            Assert.False(result.IsSuccesfuly);
        }

        [Fact]
        public async Task UpdateAsync_WithValidDto_ShouldUpdateEvent()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var testEvent = Event.Create(Guid.NewGuid(), "Old Title", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 10);
            await repo.AddAsync(testEvent);
            await repo.SaveChangesAsync();

            var dto = new EventInfoDto
            {
                Id = testEvent.Id,
                Title = "New Title",
                Description = "New Desc",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(2),
                TotalSeats = 20
            };

            var result = await service.UpdateAsync(dto);

            Assert.NotNull(result);
            Assert.True(result.IsSuccesfuly);
            var updated = await repo.GetByIdAsync(testEvent.Id);
            Assert.Equal("New Title", updated.Data.Title);
            Assert.Equal(20, updated.Data.TotalSeats);
        }

        [Fact]
        public async Task UpdateAsync_WhenUpdateFails_ShouldReturnFail()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var dto = new EventInfoDto
            {
                Id = Guid.NewGuid(),
                Title = "NonExistent",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(2),
                TotalSeats = 2,
            };

            var result = await service.UpdateAsync(dto);

            Assert.NotNull(result);
            Assert.False(result.IsSuccesfuly);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldDeleteEvent()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var testEvent = Event.Create(Guid.NewGuid(), "ToDelete", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
            await repo.AddAsync(testEvent);
            await repo.SaveChangesAsync();

            var result = await service.DeleteAsync(testEvent.Id);

            Assert.NotNull(result);
            Assert.True(result.IsSuccesfuly);
            var deleted = await repo.GetByIdAsync(testEvent.Id);
            Assert.False(deleted.IsSuccesfuly);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFail()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var result = await service.DeleteAsync(Guid.NewGuid());

            Assert.NotNull(result);
            Assert.False(result.IsSuccesfuly);
        }

        [Fact]
        public async Task AddRangeAsync_WithValidDtos_ShouldAddAllEvents()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var dtos = new List<EventInfoDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Event 1", Description = "Desc1", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(1), TotalSeats = 5 },
                new() { Id = Guid.NewGuid(), Title = "Event 2", Description = "Desc2", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(2), TotalSeats = 10 }
            };

            var result = await service.AddRangeAsync(dtos);

            Assert.NotNull(result);
            Assert.True(result.IsSuccesfuly);
            var all = await repo.GetAllAsync();
            Assert.Equal(2, all.Count());
        }

        [Fact]
        public async Task UpdateRangeAsync_WithValidDtos_ShouldUpdateAllEvents()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var ev1 = Event.Create(Guid.NewGuid(), "Old1", "Desc1", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
            var ev2 = Event.Create(Guid.NewGuid(), "Old2", "Desc2", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);
            await repo.AddRangeAsync(new[] { ev1, ev2 });
            await repo.SaveChangesAsync();

            var dtos = new List<EventInfoDto>
            {
                new() { Id = ev1.Id, Title = "New1", Description = "NewDesc1", StartAt = ev1.StartAt, EndAt = ev1.EndAt, TotalSeats = 6 },
                new() { Id = ev2.Id, Title = "New2", Description = "NewDesc2", StartAt = ev2.StartAt, EndAt = ev2.EndAt, TotalSeats = 11 }
            };

            var result = await service.UpdateRangeAsync(dtos);

            Assert.NotNull(result);
            Assert.True(result.IsSuccesfuly);
            var updated1 = await repo.GetByIdAsync(ev1.Id);
            var updated2 = await repo.GetByIdAsync(ev2.Id);
            Assert.Equal("New1", updated1.Data.Title);
            Assert.Equal(6, updated1.Data.TotalSeats);
            Assert.Equal("New2", updated2.Data.Title);
            Assert.Equal(11, updated2.Data.TotalSeats);
        }

        [Fact]
        public void IsExisted_WithValidId_ShouldReturnTrue()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var testEvent = Event.Create(Guid.NewGuid(), "Test", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
            repo.AddAsync(testEvent).Wait();
            repo.SaveChangesAsync().Wait();

            var result = service.IsExisted(testEvent.Id);

            Assert.True(result);
        }

        [Fact]
        public void IsExisted_WithInvalidId_ShouldReturnFalse()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var result = service.IsExisted(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public void IsExistedByTitle_WithValidTitle_ShouldReturnTrue()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var testEvent = Event.Create(Guid.NewGuid(), "UniqueTitle", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5);
            repo.AddAsync(testEvent).Wait();
            repo.SaveChangesAsync().Wait();

            var result = service.IsExistedByTitle("UniqueTitle");

            Assert.True(result);
        }

        [Fact]
        public void IsExistedByTitle_WithInvalidTitle_ShouldReturnFalse()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();

            var result = service.IsExistedByTitle("NonExistent");

            Assert.False(result);
        }

        [Fact]
        public async Task GetFilteredAsync_WithTitleFilter_ShouldReturnFilteredEvents()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var events = new[]
            {
                Event.Create(Guid.NewGuid(), "Alpha", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5),
                Event.Create(Guid.NewGuid(), "Beta", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10)
            };
            await repo.AddRangeAsync(events);
            await repo.SaveChangesAsync();

            var filter = new EventFilterDto { Title = "Alpha" };

            var result = await service.GetFilteredAsync(filter);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Alpha", result.Items.First().Title);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetFilteredAsync_WithDateFromFilter_ShouldReturnEventsAfterDate()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var now = DateTime.UtcNow;
            var events = new[]
            {
                Event.Create(Guid.NewGuid(), "Past", "", now.AddDays(-5), now.AddDays(-4), 5),
                Event.Create(Guid.NewGuid(), "Future", "", now.AddDays(1), now.AddDays(2), 10)
            };
            await repo.AddRangeAsync(events);
            await repo.SaveChangesAsync();

            var filter = new EventFilterDto { From = now.AddDays(-2) };

            var result = await service.GetFilteredAsync(filter);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Future", result.Items.First().Title);
        }

        [Fact]
        public async Task GetFilteredAsync_WithDateToFilter_ShouldReturnEventsBeforeDate()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var now = DateTime.UtcNow;
            var events = new[]
            {
                Event.Create(Guid.NewGuid(), "Past", "", now.AddDays(-5), now.AddDays(-4), 5),
                Event.Create(Guid.NewGuid(), "Future", "", now.AddDays(1), now.AddDays(2), 10)
            };
            await repo.AddRangeAsync(events);
            await repo.SaveChangesAsync();

            var filter = new EventFilterDto { To = now.AddDays(-3) };

            var result = await service.GetFilteredAsync(filter);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Past", result.Items.First().Title);
        }

        [Fact]
        public async Task GetFilteredAsync_WithSortByTitleAscending_ShouldReturnSortedEvents()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var events = new[]
            {
                Event.Create(Guid.NewGuid(), "Gamma", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 5),
                Event.Create(Guid.NewGuid(), "Alpha", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10),
                Event.Create(Guid.NewGuid(), "Beta", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(3), 15)
            };
            await repo.AddRangeAsync(events);
            await repo.SaveChangesAsync();

            var filter = new EventFilterDto
            {
                SortBy = "Title",
                SortDesc = false,
                PageSize = 10
            };

            var result = await service.GetFilteredAsync(filter);

            Assert.NotNull(result);
            var titles = result.Items.Select(e => e.Title).ToList();
            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, titles);
        }

        [Fact]
        public async Task GetFilteredAsync_WithPagination_ShouldReturnSecondPage()
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEventService>();
            var repo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();

            var events = Enumerable.Range(1, 5).Select(i =>
                Event.Create(Guid.NewGuid(), $"Event {i}", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(i), i * 5)
            ).ToArray();
            await repo.AddRangeAsync(events);
            await repo.SaveChangesAsync();

            var filter = new EventFilterDto
            {
                Page = 2,
                PageSize = 2
            };

            var result = await service.GetFilteredAsync(filter);

            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Page);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(3, result.TotalPages);
            Assert.Equal("Event 3", result.Items.First().Title);
        }

        #endregion
    }
}