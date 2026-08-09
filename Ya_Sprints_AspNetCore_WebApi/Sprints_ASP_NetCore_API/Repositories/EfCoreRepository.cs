using Microsoft.EntityFrameworkCore;
using SprintASP_NetCore_API.Data.DataAccess.DbContexts;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;

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

        public async Task<IQueryable<T>> GetQueryAsync() => await Task.FromResult(_dbSet.AsQueryable());
      

        public async Task<IResultEntity<T>> AddAsync(T item)
        {
            if (await _dbSet.AnyAsync(e => e.Id == item.Id))
                return ResultEntity<T>.Fail($"Модель с Id {item.Id} уже существует.");

            await _dbSet.AddAsync(item);
            await _context.SaveChangesAsync();
            return ResultEntity<T>.Ok(item, "Успешно добавлено");
        }

        public async Task<IResultEntity<T>> DeleteAsync(Guid id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null)
                return ResultEntity<T>.Fail($"Модель с Id {id} не найдена.");

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return ResultEntity<T>.Ok("Успешно удалено");
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

           
            _context.Entry(existing).State = EntityState.Detached; 
            _dbSet.Update(item);
            await _context.SaveChangesAsync();
            return ResultEntity<T>.Ok(item, "Успешно обновлено");
        }

        public async Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items)
        {
            await _dbSet.AddRangeAsync(items);
            await _context.SaveChangesAsync();
            return ResultEntity<T>.Ok("Успешно добавлено");
        }

        public async Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                var existing = await _dbSet.FindAsync(item.Id);
                if (existing == null)
                    return ResultEntity<T>.Fail($"Модель с Id {item.Id} не найдена.");
                _context.Entry(existing).State = EntityState.Detached; // опционально
            }
            _dbSet.UpdateRange(items);
            await _context.SaveChangesAsync();
            return ResultEntity<T>.Ok("Успешно обновлено");
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
