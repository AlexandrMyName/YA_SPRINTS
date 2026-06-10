using Sprint1_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;
using System.Collections.Concurrent;
using System.Security.Cryptography;


namespace Sprint1_Project_ASP_NetCore_API.Repositories
{

    public class BaseInMemoryRepository<T> : IRepository<T> where T : class, IEntity
    { 

        private ConcurrentDictionary<Guid,T> _items = new(); 

        public async Task<IResultEntity<T>> AddAsync(T item)
        {
            
            if(_items.TryGetValue(item.Id, out var result)) {
                return ResultEntity<T>.Fail($"Ошибка добавления модели <{typeof(T)}>. Модель с указанным идентификатором уже находится в коллекции");
            }
             
            if (!_items.TryAdd(item.Id, item))
            {
                //   коллизия ?
                 return ResultEntity<T>.Fail($"Ошибка добавления модели <{typeof(T)}>. Не удалось добавить модель в коллекцию");
            }

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
            result = item; 
            return ResultEntity<T>.Ok(item, "Успешно");
        }


        public bool IsExisted(Guid id) => _items.ContainsKey(id);

        public async Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items)
        {

            List<string> itemsNotAdded = new();
            foreach(var i in items)
            {
                if (!_items.TryAdd(i.Id,   i))
                {
                    itemsNotAdded.Add($"{i.Id} не добавлен в коллекцию");
                }
            }

            if (itemsNotAdded.Count > 0)
            {
                return ResultEntity<T>.Fail(string.Join(", ", itemsNotAdded));
            }
            return ResultEntity<T>.Ok( "Успешно");
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

            foreach (var i in items){
                _items.TryGetValue(i.Id, out var itemExisted);
                itemExisted = i;
            } 
            return ResultEntity<T>.Ok("Успешно");
        }
    }
}
