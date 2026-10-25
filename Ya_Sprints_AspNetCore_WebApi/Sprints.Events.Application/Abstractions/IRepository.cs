using Sprints.Events.Application.Internal;
using Sprints.Events.Application.Dtos; 
using Sprints.Events.Domain.Entities;
using System.Linq.Expressions;


namespace Sprints.Events.Application.Abstractions;


public interface IRepository<T> where T : class, IEntity
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task<IResultEntity<T>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<PaginatedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate, int page, int pageSize,
        CancellationToken ct = default);

    Task<IResultEntity<T>> AddAsync(T item, CancellationToken ct = default);
    Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items, CancellationToken ct = default);
    Task<IResultEntity<T>> UpdateAsync(T item, CancellationToken ct = default);
    Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items, CancellationToken ct = default);
    Task<IResultEntity<T>> DeleteAsync(Guid id, CancellationToken ct = default);

    bool IsExisted(Guid id);
    bool IsExistedByTitle(string name);

    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetOrderedAsync(Func<IQueryable<T>,
        IOrderedQueryable<T>> orderBy, 
        int take, 
        CancellationToken ct = default);
}
