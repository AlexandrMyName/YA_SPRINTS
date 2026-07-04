using dynamicQueryBuilder;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Dtos.Filters;

public class EventFilterDto : IEntityFilter<IEntity>
{
    // Фильтры
    public string? Title { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; } 

    // Сортировка
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = false;

    // Пагинация 
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 10;


    public IQueryable<IEntity> Apply(IQueryable<IEntity> query)
    {
        var queryEvents = query as IQueryable<IEvent>;

        if (queryEvents != null)
        {
            // 1. Применяем фильтры
            if (!string.IsNullOrEmpty(Title))
                queryEvents = DynamicQueryBuilder<IEvent>.ApplyFilter(queryEvents, nameof(Title), "contains", Title);

            if (From.HasValue)
                queryEvents = DynamicQueryBuilder<IEvent>.ApplyFilter(queryEvents, nameof(Event.StartAt), ">=", From.Value);

            if (To.HasValue)
                queryEvents = DynamicQueryBuilder<IEvent>.ApplyFilter(queryEvents, nameof(Event.EndAt), "<=", To.Value); 

            // 2. Применяем сортировку
            if (!string.IsNullOrEmpty(SortBy))
                queryEvents = DynamicQueryBuilder<IEvent>.ApplySort(queryEvents, SortBy, !SortDesc);
             
            return queryEvents;
        }

        throw new NullReferenceException("Query is not of type IQueryable<IEvent>");
    }
}