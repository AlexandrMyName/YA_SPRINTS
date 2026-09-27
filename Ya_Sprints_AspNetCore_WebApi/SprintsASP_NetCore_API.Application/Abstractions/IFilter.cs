

namespace SprintsASP_NetCore_API.Application.Abstractions;


public interface IFilter<T>
{

    string? SortBy { get; set; }
    bool SortDesc { get; set; }
    int? Page { get; set; }
    int? PageSize { get; set; }

    IQueryable<T> Apply(IQueryable<T> query);
}