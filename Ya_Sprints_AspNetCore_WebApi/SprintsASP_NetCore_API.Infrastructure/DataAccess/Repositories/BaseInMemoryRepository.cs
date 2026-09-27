using SprintsASP_NetCore_API.Infrastructure.DataAccess;
using SprintsASP_NetCore_API.Application.Abstractions; 
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Application.Dtos;
using SprintASP_NetCore_API.Domain.Entities;
using System.Collections.Concurrent;
using System.Linq.Expressions;
 


namespace SprintASP_NetCore_API.Infrastructure.Repositories;


[Obsolete("Используйте EfCoreRepository с провайдером InMemory: options.UseInMemoryDatabase(\"TestDb\")")]
public class BaseInMemoryRepository<T> : IRepository<T> where T : class, IEntity
{

    private readonly ConcurrentDictionary<Guid, T> _items = new();

    // ===================== Чтение =====================

    public Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IEnumerable<T>>(_items.Values.ToList());

    public async Task<IResultEntity<T>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_items.TryGetValue(id, out var item))
            return ResultEntity<T>.Fail(
                $"Ошибка получения модели <{typeof(T)}>. Модель с указанным идентификатором не найдена");

        return ResultEntity<T>.Ok(item, "Успешно");
    }

    public Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var compiled = predicate.Compile();
        IReadOnlyList<T> result = _items.Values.Where(compiled).ToList();
        return Task.FromResult(result);
    }

    public Task<PaginatedResult<T>> GetPagedAsync(
        Expression<Func<T, bool>>? predicate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        IEnumerable<T> query = _items.Values;

        if (predicate is not null)
        {
            var compiled = predicate.Compile();
            query = query.Where(compiled);
        }

        var total = query.Count();
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(PaginatedResult<T>.Create(items, total, page, pageSize));
    }

    // ===================== Запись =====================

    public async Task<IResultEntity<T>> AddAsync(T item, CancellationToken ct = default)
    {
        if (!_items.TryAdd(item.Id, item))
            return ResultEntity<T>.Fail(
                $"Ошибка добавления модели <{typeof(T)}>. Модель с указанным идентификатором уже находится в коллекции");

        return ResultEntity<T>.Ok(item, "Успешно");
    }

    public async Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
    {
        var notAdded = new List<string>();

        foreach (var i in items)
        {
            if (!_items.TryAdd(i.Id, i))
                notAdded.Add($"{i.Id} не добавлен в коллекцию");
        }

        if (notAdded.Count > 0)
            return ResultEntity<T>.Fail(string.Join(", ", notAdded));

        return ResultEntity<T>.Ok("Успешно");
    }

    public async Task<IResultEntity<T>> UpdateAsync(T item, CancellationToken ct = default)
    {
        if (!_items.TryGetValue(item.Id, out _))
            return ResultEntity<T>.Fail(
                $"Ошибка обновления модели <{typeof(T)}>. Модель с указанным идентификатором не найдена");

        _items[item.Id] = item;
        return ResultEntity<T>.Ok(item, "Успешно");
    }

    public async Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
    {
        var list = items.ToList();
        var notUpdated = list
            .Where(i => !_items.ContainsKey(i.Id))
            .Select(i => $"{i.Id} не существует в коллекции")
            .ToList();

        if (notUpdated.Count > 0)
            return ResultEntity<T>.Fail(string.Join(", ", notUpdated));

        foreach (var i in list)
            _items[i.Id] = i;

        return ResultEntity<T>.Ok("Успешно");
    }

    public async Task<IResultEntity<T>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_items.TryGetValue(id, out _))
            return ResultEntity<T>.Fail(
                $"Ошибка удаления модели <{typeof(T)}>. Модель с указанным идентификатором не найдена");

        if (!_items.TryRemove(id, out _))
            return ResultEntity<T>.Fail($"Ошибка удаления модели <{typeof(T)}>");

        return ResultEntity<T>.Ok("Успешно");
    }

    // ===================== Проверки =====================

    public bool IsExisted(Guid id) => _items.ContainsKey(id);

    public bool IsExistedByTitle(string name)
    {
        foreach (var item in _items.Values)
        {
            var titleProp = item.GetType().GetProperty("Title");
            if (titleProp is null) continue;

            if (titleProp.GetValue(item) is string s && string.Equals(s, name))
                return true;
        }
        return false;
    }

    // ===================== Транзакция и сохранение =====================

    public Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
        => Task.FromResult<ITransaction>(new NullTransaction());

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Task.FromResult(_items.Count);
}