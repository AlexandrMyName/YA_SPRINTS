using dynamicQueryBuilder;
using Microsoft.AspNetCore.Mvc;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

public class EventFilterDto : IEntityFilter<IEntity>
{
    [BindProperty(Name = "title")]
    public string? Title { get; set; }

    [BindProperty(Name = "from")]
    public DateTime? From { get; set; }

    [BindProperty(Name = "to")]
    public DateTime? To { get; set; }

    [BindProperty(Name = "totalSeats")]
    public int? TotalSeats { get; set; }

    [BindProperty(Name = "availableSeats")]
    public int? AvailableSeats { get; set; }

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
        var queryEvents = query as IQueryable<Event>;
        if (queryEvents == null)
            throw new NullReferenceException("Query is not of type IQueryable<Event>");

        // Фильтрация
        if (!string.IsNullOrEmpty(Title))
            queryEvents = DynamicQueryBuilder<Event>.ApplyFilter(queryEvents, nameof(Event.Title), "contains", Title);

        // Фильтр по дате начала (StartAt)
        if (From.HasValue)
            queryEvents = DynamicQueryBuilder<Event>.ApplyFilter(queryEvents, nameof(Event.StartAt), ">=", From.Value);

        if (To.HasValue)
            queryEvents = DynamicQueryBuilder<Event>.ApplyFilter(queryEvents, nameof(Event.StartAt), "<=", To.Value);

        if (TotalSeats.HasValue)
            queryEvents = DynamicQueryBuilder<Event>.ApplyFilter(queryEvents, nameof(Event.TotalSeats), "==", TotalSeats.Value);

        if (AvailableSeats.HasValue)
            queryEvents = DynamicQueryBuilder<Event>.ApplyFilter(queryEvents, nameof(Event.AvailableSeats), "==", AvailableSeats.Value);

        // Сортировка
        if (!string.IsNullOrEmpty(SortBy))
            queryEvents = DynamicQueryBuilder<Event>.ApplySort(queryEvents, SortBy, !SortDesc);

        // Пагинация
        if (Page.HasValue && PageSize.HasValue)
            queryEvents = queryEvents.Skip((Page.Value - 1) * PageSize.Value).Take(PageSize.Value);

        return queryEvents;
    }
}