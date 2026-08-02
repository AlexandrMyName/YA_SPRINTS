using dynamicQueryBuilder;
using Microsoft.AspNetCore.Mvc;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

namespace SprintASP_NetCore_API.Data.Dtos.Filters;

public class BookingFilterDto : IEntityFilter<IEntity>
{

    /// <summary>
    /// ID
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// ID события
    /// </summary>
    public Guid? EventId { get; set; }

    /// <summary>
    /// Статус обработки
    /// </summary>
    public BookingStatus? Status { get; set; }
        
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
        var queryBookings = query as IQueryable<IBooking>;

        if (queryBookings != null)
        {
            if (Id.HasValue)
                queryBookings = DynamicQueryBuilder<IBooking>.ApplyFilter(queryBookings, nameof(Booking.Id), "==", Id);

            if (EventId.HasValue)
                queryBookings = DynamicQueryBuilder<IBooking>.ApplyFilter(queryBookings, nameof(Booking.EventId), "==", EventId); 
           
            if(Status.HasValue)
                queryBookings = DynamicQueryBuilder<IBooking>.ApplyFilter(queryBookings, nameof(Booking.Status), "==", Status);
             
            // 2. Применяем сортировку
            if (!string.IsNullOrEmpty(SortBy))
                queryBookings = DynamicQueryBuilder<IBooking>.ApplySort(queryBookings, SortBy, !SortDesc);

            return queryBookings;
        }

        throw new NullReferenceException("Query is not of type IQueryable<IEvent>");
    }
}