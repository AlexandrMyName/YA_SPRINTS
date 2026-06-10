using Sprint1_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;


namespace Sprint1_Project_ASP_NetCore_API.Repositories;


public interface IRepository<T> where T : class, IEntity 
{

    Task<IEnumerable<T>> GetAllAsync();
    Task<IResultEntity<T>> GetByIdAsync(Guid id);
    Task<IResultEntity<T>> AddAsync(T item);
    Task<IResultEntity<T>> AddRangeAsync(IEnumerable<T> items);
    Task<IResultEntity<T>> UpdateAsync(T item);
    Task<IResultEntity<T>> UpdateRangeAsync(IEnumerable<T> items);
    Task<IResultEntity<T>> DeleteAsync(Guid id);

    bool IsExisted(Guid id);
}
