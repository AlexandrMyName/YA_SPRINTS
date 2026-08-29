using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using Xunit;


namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты для проверки фильтрации с библиотекой queryBuilder.lib
/// Проверяют BookingFilterDto
/// </summary>
public class BookingFilterTests : TestBase
{

    public BookingFilterTests() { }

    public override async ValueTask InitializeAsync()
    {

        await base.InitializeAsync();
        await ClearDatabaseAsync();

        var eventRepo = GetService<IRepository<Event>>();
        var bookingRepo = GetService<IRepository<Booking>>();
         
        var ev = Event.Create(Guid.NewGuid(), "Test Event", "Description", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 10);
        await eventRepo.AddAsync(ev);
        await eventRepo.SaveChangesAsync();

        // несколько броней с разными статусами
        var bookings = new List<Booking>
        {
            Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow),
            Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Confirmed, DateTime.UtcNow.AddMinutes(1)),
            Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Rejected, DateTime.UtcNow.AddMinutes(2)),
            Booking.Create(Guid.NewGuid(), ev.Id, BookingStatus.Pending, DateTime.UtcNow.AddMinutes(3)), // второй Pending
        };

        foreach (var b in bookings)
        {
            await bookingRepo.AddAsync(b);
        }
        await bookingRepo.SaveChangesAsync();
    }

    [Fact]
    public async Task FilterByStatus_ShouldReturnCorrectBookings()
    {
        var repo = GetService<IRepository<Booking>>();
        var filter = new BookingFilterDto
        {
            Status = BookingStatus.Pending,
            Page = 1,
            PageSize = 10
        };

        var query = await repo.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Booking>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }

    [Fact]
    public async Task FilterByEventId_ShouldReturnBookingsForEvent()
    {
        var repo = GetService<IRepository<Booking>>();

        // Получаем ID события, которое мы создали в InitializeAsync
        var eventRepo = GetService<IRepository<Event>>();
        var ev = (await eventRepo.GetAllAsync()).FirstOrDefault();
        Assert.NotNull(ev);

        var filter = new BookingFilterDto
        {
            EventId = ev.Id,
            Page = 1,
            PageSize = 10
        };

        var query = await repo.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Booking>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(4, result.Count); // все брони принадлежат этому событию
        Assert.All(result, b => Assert.Equal(ev.Id, b.EventId));
    }

    [Fact]
    public async Task SortByCreatedAtDesc_ShouldReturnSortedBookings()
    {
        var repo = GetService<IRepository<Booking>>();
        var filter = new BookingFilterDto
        {
            SortBy = nameof(Booking.CreatedAt),
            SortDesc = true,
            Page = 1,
            PageSize = 10
        };

        var query = await repo.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Booking>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(4, result.Count);

        // Проверка, что CreatedAt убывает
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].CreatedAt >= result[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task Pagination_ShouldReturnCorrectPage()
    {
        var repo = GetService<IRepository<Booking>>();
        var filter = new BookingFilterDto
        {
            Page = 2,
            PageSize = 2,
            SortBy = nameof(Booking.CreatedAt),
            SortDesc = false // по возрастанию CreatedAt (самые старые первые)
        };

        var query = await repo.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Booking>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Equal(2, result.Count);
        // Ожидаем, что на второй странице будут две брони с самыми поздними CreatedAt (при сортировке по возрастанию)
        // Всего 4 брони, страница 2 размер 2 => 3-я и 4-я по порядку.
        // Так как CreatedAt: первая Pending (UtcNow), вторая Confirmed (UtcNow+1мин), третья Rejected (UtcNow+2мин), четвертая Pending (UtcNow+3мин)
        // При сортировке по возрастанию: 1-я, 2-я, 3-я, 4-я. Вторая страница = 3-я и 4-я.
        Assert.Equal(BookingStatus.Rejected, result[0].Status);
        Assert.Equal(BookingStatus.Pending, result[1].Status); // четвёртая
    }

    [Fact]
    public async Task FilterByStatusAndPagination_ShouldWorkTogether()
    {
        var repo = GetService<IRepository<Booking>>();
        var filter = new BookingFilterDto
        {
            Status = BookingStatus.Pending,
            Page = 1,
            PageSize = 1,
            SortBy = nameof(Booking.CreatedAt),
            SortDesc = false
        };

        var query = await repo.GetQueryAsync();
        var filteredQuery = filter.Apply(query) as IQueryable<Booking>;
        Assert.NotNull(filteredQuery);
        var result = filteredQuery.ToList();

        Assert.Single(result);
        // Ожидаем, что будет первая Pending (самая ранняя)
        // Всего две Pending: CreatedAt UtcNow и UtcNow+3мин. Первая – UtcNow.
        Assert.Equal(BookingStatus.Pending, result[0].Status);
       
        Assert.True(result[0].CreatedAt < DateTime.UtcNow.AddMinutes(2));
    }
}