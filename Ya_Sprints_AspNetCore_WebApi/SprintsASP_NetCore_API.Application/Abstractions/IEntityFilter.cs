using Sprints_Project_ASP_NetCore_API.Data.Entities; 
using System.Linq.Expressions; 


namespace SprintsASP_NetCore_API.Application.Abstractions;


public interface IEntityFilter<T> where T : class, IEntity
{
    int? Page { get; }
    int? PageSize { get; }
    Expression<Func<T, bool>> ToPredicate();
}