using Microsoft.EntityFrameworkCore.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Data.Dtos.Filters;


namespace Sprints_Project_ASP_NetCore_API.Services;

/// <summary>
/// Интерфейс для сервиса взаимодействия с данными 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDataStorageService<T> where T : class, IEntityDto
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<PaginatedResult<T>> GetFilteredAsync(IEntityFilter<IEntity> filter);

    Task<IResultDto<T>> GetByIdAsync(Guid id);
    Task<IResultDto<T>> AddAsync(T item);
    Task<IResultDto<T>> AddRangeAsync(IEnumerable<T> items);
    Task<IResultDto<T>> UpdateAsync(T item);
    Task<IResultDto<T>> UpdateRangeAsync(IEnumerable<T> items);
    Task<IResultDto<T>> DeleteAsync(Guid id);
    bool IsExisted(Guid id);
    bool IsExistedByTitle(string name);
}
