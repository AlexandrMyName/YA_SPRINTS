using SprintASP_NetCore_API.Domain.Entities; 
using System.Linq.Expressions; 


namespace SprintsASP_NetCore_API.Application.Abstractions;


public interface IEntityFilter<T> where T : class, IEntity
{
    int? Page { get; }
    int? PageSize { get; }
    Expression<Func<T, bool>> ToPredicate();
}