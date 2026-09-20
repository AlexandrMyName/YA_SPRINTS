using SprintASP_NetCore_API.Data.Dtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using System.Linq.Expressions;


namespace SprintsASP_NetCore_API.Application.Abstractions;


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
}
