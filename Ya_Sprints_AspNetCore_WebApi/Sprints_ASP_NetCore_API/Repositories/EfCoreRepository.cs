using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions; 


namespace SprintASP_NetCore_API.Repositories
{

    public class EfCoreRepository<T> : IRepository<T> where T : class, IEntity
    {

        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public EfCoreRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        /// <summary>
        /// Флаг поддержки транзакций
        /// </summary>
        private bool SupportsTransactions => _context.Database.IsRelational();
         
        /// <summary>
        /// Массовое обновление записей, удовлетворяющих фильтру, без загрузки в память.
        /// Подходит для массовых обновлений, генерирует один SQL-запрос UPDATE.
        /// Серверная логика. <b>Версионность не проверяется</b> – при необходимости включите проверку версии в условие фильтра.
        /// </summary>
        /// <example>
        /// Пример обновления количества доступных мест с проверкой версии:
        /// <code>
        /// var updatedCount = await _eventRepository.UpdateBatchAsync(
        ///     e => e.Id == eventId &amp;&amp; e.Version == currentVersion,
        ///     updates => updates.SetProperty(e => e.AvailableSeats, newValue)
        /// );
        /// if (updatedCount == 0)
        /// {
        ///     // Конфликт версий или запись не найдена
        /// }
        /// </code>
        /// </example>
        /// <param name="filter">Условие для отбора записей</param>
        /// <param name="setPropertyCalls">Делегат для установки свойств (используйте SetProperty)</param>
        /// <returns>Количество обновленных записей</returns>
        public async Task<int> UpdateBatchAsync(
            Expression<Func<T, bool>> filter,
            Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls)
        {
            return await _dbSet
                .Where(filter)
                .ExecuteUpdateAsync(setPropertyCalls);
        }
         

        // Для этой логики чуть больше потребуется времени. В будущем реализую  
        public async Task<int> UpdateBatchWithVersionListAsync(
            Dictionary<Guid, uint> idVersionMap, // Id -> ожидаемая версия
            Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls)
        {
            
            var conditions = string.Join(" OR ", idVersionMap.Select(kvp =>
                $"(Id = '{kvp.Key}' AND Version = {kvp.Value})"));
            var sql = $"UPDATE {typeof(T).Name} SET ... WHERE {conditions}";
            return await _context.Database.ExecuteSqlRawAsync(sql);
        }


        /// <summary>
        /// Начало транзакции
        /// </summary>
        /// <returns></returns>
        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => SupportsTransactions
                ? await _context.Database.BeginTransactionAsync()
                : new NullDbContextTransaction(); 
         
        /// <summary>
        /// Явное сохранения  
        /// </summary>
        /// <returns></returns>
        public async Task<int> SaveChangesAsync() {
            try
            {
               return await _context.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException concurrencyException)
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
            return ResultEntity<T>.Ok(item, $"Успешно добавлено {item}");
        }

        public async Task<IResultEntity<T>> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return ResultEntity<T>.Fail($"Модель с Id {id} не найдена.");

            _dbSet.Remove(entity); 
            return ResultEntity<T>.Ok($"Успешно удалено: {entity}");
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
             
            var currentVersion = existing.Version;
             
            if (currentVersion != item.Version)
                return ResultEntity<T>.Fail("Конфликт версий: данные были изменены другим пользователем.");
             
            _context.Entry(existing).CurrentValues.SetValues(item);
             
            existing.Version = currentVersion; // Остаётся для ясности

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

                // Проверяем версию
                if (existing.Version != item.Version)
                    return ResultEntity<T>.Fail($"Конфликт версий для Id {item.Id}: данные были изменены другим пользователем.");

                _context.Entry(existing).CurrentValues.SetValues(item);
              
                 existing.Version = existing.Version; // Остаётся для ясности
            }
            return ResultEntity<T>.Ok("Успешно обновлено (изменения не сохранены)");
        }


        public bool IsExisted(Guid id)
        {
            return _dbSet.Any(e => e.Id == id);
        }


        public bool IsExistedByTitle(string name)
        {
            // Для универсальности лучше передавать выражение, но для демонстрации:
            return _dbSet.Any(e => EF.Property<string>(e, "Title") == name);
        }

    }
}
