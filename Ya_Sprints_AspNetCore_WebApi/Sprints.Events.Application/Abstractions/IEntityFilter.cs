using Sprints.Events.Domain.Entities; 
using System.Linq.Expressions; 


namespace Sprints.Events.Application.Abstractions;


public interface IEntityFilter<T> where T : class, IEntity
{
    int? Page { get; }
    int? PageSize { get; }
    Expression<Func<T, bool>> ToPredicate();
}