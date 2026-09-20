 
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using SprintsASP_NetCore_API.Application.Abstractions;
using System.Linq.Expressions;


namespace SprintASP_NetCore_API.Data.Dtos.Filters;


public class BookingFilterDto : IEntityFilter<Booking>
{

    public Guid? Id { get; set; }
    public Guid? EventId { get; set; }
    public BookingStatus? Status { get; set; }

    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = false;

    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 10;

    public Expression<Func<Booking, bool>> ToPredicate()
    {
        return b =>
            (!Id.HasValue || b.Id == Id.Value) &&
            (!EventId.HasValue || b.EventId == EventId.Value) &&
            (!Status.HasValue || b.Status == Status.Value);
    }
}