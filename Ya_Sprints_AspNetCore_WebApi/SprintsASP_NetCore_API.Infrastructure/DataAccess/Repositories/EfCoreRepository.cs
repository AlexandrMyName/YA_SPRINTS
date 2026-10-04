using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintsASP_NetCore_API.Infrastructure.DataAccess; 
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Application.Dtos;
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
  

namespace SprintASP_NetCore_API.Infrastructure.Repositories;


public class EfCoreRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public EfCoreRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    private bool SupportsTransactions => _context.Database.IsRelational();

    // ===================== Чтение =====================

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.AsNoTracking().ToListAsync(ct);

    public async Task<IResultEntity<T>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, ct);
        return entity is null
            ? ResultEntity<T>.Fail($"Модель с Id {id} не найдена.")
            : ResultEntity<T>.Ok(entity, "Успешно");
    }

    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<PaginatedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        IQueryable<T> query = _dbSet.AsNoTracking();
        if (predicate is not null) query = query.Where(predicate);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return PaginatedResult<T>.Create(items, total, page, pageSize);
    }

    // ===================== Запись =====================

    public async Task<IResultEntity<T>> AddAsync(T item, CancellationToken ct = default)
    {
        if (await _dbSet.AnyAsync(e => e.Id == item.Id, ct))
            return ResultEntity<T>.Fail($"Модель с Id {item.Id} уже существует.");

        await _dbSet.AddAsync(item, ct);
        return ResultEntity<T>.Ok(item, $"Успешно добавлено {item.Id}");
    }

    public async Task<IResultEntity<T>> AddRangeAsync(
        IEnumerable<T> items, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(items, ct);
        return ResultEntity<T>.Ok("Успешно добавлено");
    }

    public async Task<IResultEntity<T>> UpdateAsync(T item, CancellationToken ct = default)
    {
        var existing = await _dbSet.FindAsync(new object[] { item.Id }, ct);
        if (existing is null)
            return ResultEntity<T>.Fail($"Модель с Id {item.Id} не найдена.");

        _context.Entry(existing).CurrentValues.SetValues(item);
        return ResultEntity<T>.Ok(item, "Успешно обновлено (изменения не сохранены)");
    }

    public async Task<IResultEntity<T>> UpdateRangeAsync(
        IEnumerable<T> items, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            var existing = await _dbSet.FindAsync(new object[] { item.Id }, ct);
            if (existing is null)
                return ResultEntity<T>.Fail($"Модель с Id {item.Id} не найдена.");

            _context.Entry(existing).CurrentValues.SetValues(item);
        }
        return ResultEntity<T>.Ok("Успешно обновлено (изменения не сохранены)");
    }

    public async Task<IResultEntity<T>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, ct);
        if (entity is null)
            return ResultEntity<T>.Fail($"Модель с Id {id} не найдена.");

        _dbSet.Remove(entity);
        return ResultEntity<T>.Ok($"Успешно удалено: {entity.Id}");
    }

    // ===================== Проверки =====================

    public bool IsExisted(Guid id) => _dbSet.Any(e => e.Id == id);

    public bool IsExistedByTitle(string name)
        => _dbSet.Any(e => EF.Property<string>(e, "Title") == name);

    // ===================== Транзакция и сохранение =====================

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (!SupportsTransactions)
            return new NullTransaction();

        var tx = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransaction(tx);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}