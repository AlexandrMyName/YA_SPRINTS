using Sprints_Project_ASP_NetCore_API.Application.Dtos.EntitiesDtos;
using SprintsASP_NetCore_API.Application.Abstractions; 
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Application.Dtos;
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;

/// <summary>
/// Интерфейс для сервиса взаимодействия с данными 
/// </summary>
/// <typeparam name="T"></typeparam>

public interface IDataStorageService<TDto, TEntity>
    where TDto : class, IEntityDto
    where TEntity : class, IEntity
{
    Task<IEnumerable<TDto>> GetAllAsync();
    Task<PaginatedResult<TDto>> GetFilteredAsync(IEntityFilter<TEntity> filter);

    Task<IResultDto<TDto>> GetByIdAsync(Guid id);
    Task<IResultDto<TDto>> AddAsync(TDto item);
    Task<IResultDto<TDto>> AddRangeAsync(IEnumerable<TDto> items);
    Task<IResultDto<TDto>> UpdateAsync(TDto item);
    Task<IResultDto<TDto>> UpdateRangeAsync(IEnumerable<TDto> items);
    Task<IResultDto<TDto>> DeleteAsync(Guid id);

    bool IsExisted(Guid id);
    bool IsExistedByTitle(string name);
}
