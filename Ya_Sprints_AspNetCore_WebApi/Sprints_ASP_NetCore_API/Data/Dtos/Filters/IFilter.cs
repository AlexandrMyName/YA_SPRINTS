

namespace SprintASP_NetCore_API.Data.Dtos.Filters;


public interface IFilter<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}