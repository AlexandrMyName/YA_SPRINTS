using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using System.Linq.Expressions;


namespace Sprints_Project_ASP_NetCore_API.Repositories;


public interface IRepository<T> where T : class, IEntity
{

    Task<IQueryable<T>> GetQueryAsync();
    Task<IEnumerable<T>> GetAllAsync();
    Task<IResultEntity<T>> GetByIdAsync(Guid id);
    Task<IResultEntity<T>> AddAsync(T item);
    Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items);
    Task<IResultEntity<T>> UpdateAsync(T item);
    Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items);
    Task<IResultEntity<T>> DeleteAsync(Guid id);
     
    bool IsExisted(Guid id);
    bool IsExistedByTitle(string name);


    /// <summary>
    /// Массовое обновление записей, удовлетворяющих фильтру, без загрузки в память.
    /// Серверная логика. [Версионность не проверяется]
    /// Генерирует один SQL-запрос UPDATE.
    /// </summary>
    /// <param name="filter">Условие для отбора записей</param>
    /// <param name="setPropertyCalls">Делегат для установки свойств (используйте SetProperty)</param>
    /// <returns>Количество обновленных записей</returns>
    Task<int> UpdateBatchAsync(
        Expression<Func<T, bool>> filter,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls);
     
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync(); // или Task<IResultEntity<int>>, если нужна обёртка
}
