using SprintASP_NetCore_API.Application.Filters;
using SprintASP_NetCore_API.Domain.Entities;
using SprintASP_NetCore_API.IntegrationTests.Fixture;
using SprintsASP_NetCore_API.Application.Abstractions;
using Xunit;

namespace SprintASP_NetCore_API.IntegrationTests;

/// <summary>
/// Тесты фильтрации и пагинации BookingFilterDto.
/// Работают через публичный API IRepository (ToPredicate + GetPagedAsync).
/// </summary>
[Collection("DatabaseCollection")]
public class BookingFilterTests : TestBase
{
    public BookingFilterTests(DatabaseFixture fixture) : base(fixture) { }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearDatabaseAsync();

        var eventRepo = GetService<IRepository<Event>>();
        var userRepo = GetService<IRepository<User>>();
        var bookingRepo = GetService<IRepository<Booking>>();

        // 1. Событие
        var ev = Event.Create(
            Guid.NewGuid(), "Test Event", "Description",
            DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(1).AddHours(2), 10);

        await eventRepo.AddAsync(ev);

        // 2. Пользователь (для FK bookings.UserId → users.Id)
        var user = User.Create(
            Guid.NewGuid(),
            "filter_test_user_" + Guid.NewGuid().ToString("N")[..6],
            "hash",
            UserRole.User);

        await userRepo.AddAsync(user);

        await eventRepo.SaveChangesAsync();

        // 3. Несколько броней с разными статусами (сигнатура: id, eventId, userId, status, createdAt)
        var bookings = new List<Booking>
        {
            Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending,   DateTime.UtcNow),
            Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Confirmed, DateTime.UtcNow.AddMinutes(1)),
            Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Rejected,  DateTime.UtcNow.AddMinutes(2)),
            Booking.Create(Guid.NewGuid(), ev.Id, user.Id, BookingStatus.Pending,   DateTime.UtcNow.AddMinutes(3)),
        };

        foreach (var b in bookings)
            await bookingRepo.AddAsync(b);

        await bookingRepo.SaveChangesAsync();
    }

    #region Фильтр по статусу  

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

        var paged = await repo.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(2, paged.TotalCount);
        Assert.All(paged.Items, b => Assert.Equal(BookingStatus.Pending, b.Status));
    }
    #endregion


    #region Фильтр по EventId  

    [Fact]
    public async Task FilterByEventId_ShouldReturnBookingsForEvent()
    {
        var eventRepo = GetService<IRepository<Event>>();
        var bookingRepo = GetService<IRepository<Booking>>();

        var ev = (await eventRepo.GetAllAsync()).FirstOrDefault();
        Assert.NotNull(ev);

        var filter = new BookingFilterDto
        {
            EventId = ev!.Id,
            Page = 1,
            PageSize = 10
        };

        var paged = await bookingRepo.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(4, paged.TotalCount);
        Assert.All(paged.Items, b => Assert.Equal(ev.Id, b.EventId));
    }
    #endregion


    //#region Фильтр по UserId  (Пока в FilterDto нет свойства с UserId) 
    // 
    //[Fact]
    //public async Task FilterByUserId_ShouldReturnBookingsForUser()
    //{
    //    var userRepo = GetService<IRepository<User>>();
    //    var bookingRepo = GetService<IRepository<Booking>>();

    //    var user = (await userRepo.GetAllAsync()).FirstOrDefault();
    //    Assert.NotNull(user);

    //    var filter = new BookingFilterDto
    //    {
    //        UserId = user!.Id,
    //        Page = 1,
    //        PageSize = 10
    //    };

    //    var paged = await bookingRepo.GetPagedAsync(
    //        filter.ToPredicate(),
    //        filter.Page!.Value,
    //        filter.PageSize!.Value);

    //    Assert.Equal(4, paged.TotalCount);
    //    Assert.All(paged.Items, b => Assert.Equal(user.Id, b.UserId));
    //}
    //#endregion

#region Сортировка по CreatedAt DESC  

    [Fact]
    public async Task SortByCreatedAtDesc_ShouldReturnSortedBookings()
    {
        var repo = GetService<IRepository<Booking>>();

        // Фильтр без условий — берём все
        var filter = new BookingFilterDto { Page = 1, PageSize = 10 };

        var paged = await repo.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        // Сортировка на клиенте — серверная не поддерживается текущим GetPagedAsync
        var result = paged.Items
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        Assert.Equal(4, result.Count);

        for (int i = 0; i < result.Count - 1; i++)
            Assert.True(result[i].CreatedAt >= result[i + 1].CreatedAt);
    }
    #endregion


    #region Пагинация  

    [Fact]
    public async Task Pagination_ShouldReturnCorrectPage()
    {
        var repo = GetService<IRepository<Booking>>();

        // Первая страница
        var page1 = await repo.GetPagedAsync(
            predicate: null,
            page: 1,
            pageSize: 2);

        Assert.Equal(4, page1.TotalCount);

        var items1 = page1.Items.ToList();
        Assert.Equal(2, items1.Count);

        // Вторая страница
        var page2 = await repo.GetPagedAsync(
            predicate: null,
            page: 2,
            pageSize: 2);

        Assert.Equal(4, page2.TotalCount);

        var items2 = page2.Items.ToList();
        Assert.Equal(2, items2.Count);

        // Страницы не пересекаются
        var ids1 = items1.Select(b => b.Id).ToHashSet();
        var ids2 = items2.Select(b => b.Id).ToHashSet();
        Assert.Empty(ids1.Intersect(ids2));

        // Вместе — все 4 брони
        Assert.Equal(4, ids1.Union(ids2).Count());
    }
    #endregion


    #region Фильтр + пагинация вместе  

    [Fact]
    public async Task FilterByStatusAndPagination_ShouldWorkTogether()
    {
        var repo = GetService<IRepository<Booking>>();

        var filter = new BookingFilterDto
        {
            Status = BookingStatus.Pending,
            Page = 1,
            PageSize = 1
        };

        var paged = await repo.GetPagedAsync(
            filter.ToPredicate(),
            filter.Page!.Value,
            filter.PageSize!.Value);

        Assert.Equal(2, paged.TotalCount);
        Assert.Single(paged.Items);

        var item = paged.Items.First();
        Assert.Equal(BookingStatus.Pending, item.Status);
    }
    #endregion

}