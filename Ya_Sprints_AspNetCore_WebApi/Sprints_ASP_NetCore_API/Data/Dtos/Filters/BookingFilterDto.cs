using dynamicQueryBuilder;
using Microsoft.AspNetCore.Mvc;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

namespace SprintASP_NetCore_API.Data.Dtos.Filters;

public class BookingFilterDto : IEntityFilter<IEntity>
{

    [BindProperty(Name = "id")]
    public Guid? Id { get; set; }

    [BindProperty(Name = "eventId")]
    public Guid? EventId { get; set; }

    [BindProperty(Name = "status")]
    public BookingStatus? Status { get; set; }

    [BindProperty(Name = "sortBy")]
    public string? SortBy { get; set; }

    [BindProperty(Name = "sortDesc")]
    public bool SortDesc { get; set; } = false;

    [BindProperty(Name = "page")]
    public int? Page { get; set; } = 1;

    [BindProperty(Name = "pageSize")]
    public int? PageSize { get; set; } = 10;

    public IQueryable<IEntity> Apply(IQueryable<IEntity> query)
    {
        var queryBookings = query as IQueryable<Booking>;   // используем конкретный тип
        if (queryBookings == null)
            throw new NullReferenceException("Query is not of type IQueryable<Booking>");

        // Фильтрация
        if (Id.HasValue)
            queryBookings = DynamicQueryBuilder<Booking>.ApplyFilter(queryBookings, nameof(Booking.Id), "==", Id);

        if (EventId.HasValue)
            queryBookings = DynamicQueryBuilder<Booking>.ApplyFilter(queryBookings, nameof(Booking.EventId), "==", EventId);

        if (Status.HasValue)
            queryBookings = DynamicQueryBuilder<Booking>.ApplyFilter(queryBookings, nameof(Booking.Status), "==", Status);

        // Сортировка
        if (!string.IsNullOrEmpty(SortBy))
            queryBookings = DynamicQueryBuilder<Booking>.ApplySort(queryBookings, SortBy, !SortDesc);

        // Пагинация
        if (Page.HasValue && PageSize.HasValue)
            queryBookings = queryBookings.Skip((Page.Value - 1) * PageSize.Value).Take(PageSize.Value);

        return queryBookings;
    }
}