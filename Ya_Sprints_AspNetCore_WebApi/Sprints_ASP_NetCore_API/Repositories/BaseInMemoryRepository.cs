using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Concurrent;
using System.Linq.Expressions;


namespace Sprints_Project_ASP_NetCore_API.Repositories
{

    [Obsolete("Используете EFCoreRepository с опцией: options =>\r\n    options.UseInMemoryDatabase(\"TestDb\")")]
    public class BaseInMemoryRepository<T> : IRepository<T> where T : class, IEntity
    {

        private ConcurrentDictionary<Guid, T> _items = new();
        private ReaderWriterLockSlim _locker = new();
 
        public Task<IQueryable<T>> GetQueryAsync() => Task.FromResult(_items.Values.AsQueryable());

        public async Task<IResultEntity<T>> AddAsync(T item)
        {
            _locker.EnterReadLock();
            if (_items.TryGetValue(item.Id, out var result))
            {
                _locker.ExitReadLock();
                return ResultEntity<T>.Fail($"Ошибка добавления модели <{typeof(T)}>. Модель с указанным идентификатором уже находится в коллекции");
            }
            _locker.ExitReadLock();

            _locker.EnterWriteLock();
            if (!_items.TryAdd(item.Id, item))
            {
                _locker.ExitWriteLock();
                //   коллизия ?
                return ResultEntity<T>.Fail($"Ошибка добавления модели <{typeof(T)}>. Не удалось добавить модель в коллекцию");
            }
            _locker.ExitWriteLock();
            return ResultEntity<T>.Ok(item, "Успешно");
        }


        public async Task<IResultEntity<T>> DeleteAsync(Guid id)
        {

            if (!_items.TryGetValue(id, out var result))
            {
                return ResultEntity<T>.Fail($"Ошибка удаления модели <{typeof(T)}>. Модель с указанным идентификатором не найдена");
            }

            if (!_items.Remove(id, out var value))
            {
                //   коллизия ?
                return ResultEntity<T>.Fail($"Ошибка удаления модели <{typeof(T)}>");
            }

            return ResultEntity<T>.Ok("Успешно");
        }

        public async Task<IEnumerable<T>> GetAllAsync() => _items.Values.ToList();


        public async Task<IResultEntity<T>> GetByIdAsync(Guid id)
        {
            if (!_items.TryGetValue(id, out var result))
            {
                return ResultEntity<T>.Fail($"Ошибка получения модели <{typeof(T)}>. Модель с указанным идентификатором не найдена");
            }
            return ResultEntity<T>.Ok(result, "Успешно");
        }


        public async Task<IResultEntity<T>> UpdateAsync(T item)
        {

            if (!_items.TryGetValue(item.Id, out var result))
            {
                return ResultEntity<T>.Fail($"Ошибка обновления модели <{typeof(T)}>. Модель с указанным идентификатором не найдена");
            }
            _items[item.Id] = item;

            return ResultEntity<T>.Ok(item, "Успешно");
        }




        public async Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items)
        {

            List<string> itemsNotAdded = new();
            foreach (var i in items)
            {
                if (!_items.TryAdd(i.Id, i))
                {
                    itemsNotAdded.Add($"{i.Id} не добавлен в коллекцию");
                }
            }

            if (itemsNotAdded.Count > 0)
            {
                return ResultEntity<T>.Fail(string.Join(", ", itemsNotAdded));
            }
            return ResultEntity<T>.Ok("Успешно");
        }


        public async Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items)
        {

            List<string> itemsNotUpdated = new();
            foreach (var i in items)
            {
                if (!_items.TryGetValue(i.Id, out var itemExisted))
                {
                    itemsNotUpdated.Add($"{i.Id} не существует в коллекции");
                }
            }

            if (itemsNotUpdated.Count > 0)
            {
                return ResultEntity<T>.Fail(string.Join(", ", itemsNotUpdated));
            }

            foreach (var i in items)
            {
                _items[i.Id] = i;
            }
            return ResultEntity<T>.Ok("Успешно");
        }


        public bool IsExisted(Guid id) => _items.ContainsKey(id);

        public bool IsExistedByTitle(string name)
        {

            foreach (var i in _items)
            {

                var properties = i.GetType().GetProperties();

                var titleProperty = properties.Where(p => string.Equals(p.Name, "Title")).FirstOrDefault();

                if (titleProperty != null)
                {

                    var value = titleProperty.GetValue(i);

                    if (value != null && value is string vStr)
                    {
                        if (string.Equals(vStr, name))
                        {
                            return true;
                        }
                    }
                }

            }
            return false;
        }

        public Task<int> UpdateBatchAsync(Expression<Func<T, bool>> filter, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls) => Task.FromResult(_items.Count); // Заглушка 

        public async Task<IDbContextTransaction> BeginTransactionAsync() => new NullDbContextTransaction(); // Заглушка
      
        public Task<int> SaveChangesAsync() =>  Task.FromResult(_items.Count); // Заглушка 

    }



    public class NullDbContextTransaction : IDbContextTransaction
    {
        public Guid TransactionId => Guid.Empty;

        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
