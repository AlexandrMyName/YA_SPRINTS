using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;


namespace Sprint1_Project_ASP_NetCore_API.Services;

/// <summary>
/// Интерфейс для сервиса взаимодействия с данными 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDataStorageService<T> where T : class, IEntityDto
{

    Task<IEnumerable<T>> GetAllAsync();
    Task<IResultDto<T>> GetByIdAsync(Guid id);
    Task<IResultDto<T>> AddAsync(T item); 
    Task<IResultDto<T>> AddRangeAsync(IEnumerable<T> items); 
    Task<IResultDto<T>> UpdateAsync(T item); 
    Task<IResultDto<T>> UpdateRangeAsync(IEnumerable<T> items); 
    Task<IResultDto<T>> DeleteAsync(Guid id); 
    bool IsExisted(Guid id); 
    bool IsExistedByTitle(string name);
}
