using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;


namespace SprintASP_NetCore_API.Repositories;


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

    public async Task<int> UpdateBatchAsync(
        Expression<Func<T, bool>> filter,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls)
    {
        return await _dbSet
            .Where(filter)
            .ExecuteUpdateAsync(setPropertyCalls);
    }

    public async Task<int> UpdateBatchWithVersionListAsync(
        Dictionary<Guid, uint> idVersionMap,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls)
    {
        var conditions = string.Join(" OR ", idVersionMap.Select(kvp =>
            $"(Id = '{kvp.Key}' AND Version = {kvp.Value})"));
        var sql = $"UPDATE {typeof(T).Name} SET ... WHERE {conditions}";
        return await _context.Database.ExecuteSqlRawAsync(sql);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
        => SupportsTransactions
            ? await _context.Database.BeginTransactionAsync()
            : new NullDbContextTransaction();

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw;
        }
    }

    public async Task<IQueryable<T>> GetQueryAsync() => await Task.FromResult(_dbSet.AsQueryable());

    public async Task<IResultEntity<T>> AddAsync(T item)
    {

        if (await _dbSet.AnyAsync(e => e.Id == item.Id))
            return ResultEntity<T>.Fail($"Модель с Id {item.Id} уже существует.");

        await _dbSet.AddAsync(item);
        return ResultEntity<T>.Ok(item, $"Успешно добавлено {item.Id}");
    }

    public async Task<IResultEntity<T>> DeleteAsync(Guid id)
    {

        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
            return ResultEntity<T>.Fail($"Модель с Id {id} не найдена.");

        _dbSet.Remove(entity);
        return ResultEntity<T>.Ok($"Успешно удалено: {entity.Id}");
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {

        return await _dbSet.ToListAsync();
    }

    public async Task<IResultEntity<T>> GetByIdAsync(Guid id)
    {

        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
            return ResultEntity<T>.Fail($"Модель с Id {id} не найдена.");
        return ResultEntity<T>.Ok(entity, "Успешно");
    }

    public async Task<IResultEntity<T>> UpdateAsync(T item)
    {

        var existing = await _dbSet.FindAsync(item.Id);
        if (existing == null)
            return ResultEntity<T>.Fail($"Модель с Id {item.Id} не найдена.");
         
        _context.Entry(existing).CurrentValues.SetValues(item);

        return ResultEntity<T>.Ok(item, "Успешно обновлено (изменения не сохранены)");
    }

    public async Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items)
    {
        await _dbSet.AddRangeAsync(items);
        return ResultEntity<T>.Ok("Успешно добавлено");
    }

    public async Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items)
    {
        var itemsList = items.ToList();
        foreach (var item in itemsList)
        {
            var existing = await _dbSet.FindAsync(item.Id);
            if (existing == null)
                return ResultEntity<T>.Fail($"Модель с Id {item.Id} не найдена.");

            _context.Entry(existing).CurrentValues.SetValues(item);
        }
        return ResultEntity<T>.Ok("Успешно обновлено (изменения не сохранены)");
    }

    public bool IsExisted(Guid id)
    {
        return _dbSet.Any(e => e.Id == id);
    }

    public bool IsExistedByTitle(string name)
    {
        return _dbSet.Any(e => EF.Property<string>(e, "Title") == name);
    }
}