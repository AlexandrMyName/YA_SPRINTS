using dynamicQueryBuilder;
using Microsoft.AspNetCore.Mvc;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

public class EventFilterDto : IEntityFilter<IEntity>
{

    /// <summary>
    /// Фильтр по названию события (регистронезависимый поиск по части слова)
    /// </summary>
    /// <example>Встреча</example>
    [BindProperty(Name = "title")]
    public string? Title { get; set; }

    /// <summary>
    /// Фильтр по дате начала (события, начинающиеся с указанной даты или позже)
    /// </summary>
    /// <example>2024-01-15T10:00:00</example>
    [BindProperty(Name = "from")]
    public DateTime? From { get; set; }

    /// <summary>
    /// Фильтр по дате окончания (события, заканчивающиеся до указанной даты)
    /// </summary>
    /// <example>2024-01-20T18:00:00</example>
    [BindProperty(Name = "to")]
    public DateTime? To { get; set; }

    /// <summary>
    /// Поле для сортировки (Title, StartAt, EndAt, Priority)
    /// </summary>
    /// <example>StartAt</example>
    [BindProperty(Name = "sortBy")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Направление сортировки: true — по убыванию, false — по возрастанию
    /// </summary>
    /// <example>false</example>
    [BindProperty(Name = "sortDesc")]
    public bool SortDesc { get; set; } = false;

    /// <summary>
    /// Номер страницы для пагинации (начиная с 1)
    /// </summary>
    /// <example>1</example>
    [BindProperty(Name = "page")]
    public int? Page { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице (максимум 100)
    /// </summary>
    /// <example>10</example>
    [BindProperty(Name = "pageSize")]
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