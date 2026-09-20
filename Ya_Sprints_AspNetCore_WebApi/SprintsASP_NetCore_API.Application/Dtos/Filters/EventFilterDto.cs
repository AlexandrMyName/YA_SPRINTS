using Sprints_Project_ASP_NetCore_API.Data.Entities;
using SprintsASP_NetCore_API.Application.Abstractions;
using System.Linq.Expressions;



namespace SprintASP_NetCore_API.Data.Dtos.Filters;


public class EventFilterDto : IEntityFilter<Event>
{
    public string? Title { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int? TotalSeats { get; set; }
    public int? AvailableSeats { get; set; }

    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = false;

    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 10;

    public Expression<Func<Event, bool>> ToPredicate()
    {
        return e =>
            (string.IsNullOrEmpty(Title) || e.Title.Contains(Title)) &&
            (!From.HasValue || e.StartAt >= From.Value) &&
            (!To.HasValue || e.StartAt <= To.Value) &&
            (!TotalSeats.HasValue || e.TotalSeats == TotalSeats.Value) &&
            (!AvailableSeats.HasValue || e.AvailableSeats == AvailableSeats.Value);
    }
}