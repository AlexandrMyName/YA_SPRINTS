using Sprints.Events.Application.Dtos.EntitiesDtos;
using Sprints.Events.Application.Abstractions; 
using Sprints.Events.Application.Internal;
using Sprints.Events.Application.Dtos;
using Sprints.Events.Domain.Entities;


namespace Sprints.Events.Application.UseCases.DataServices.Contracts;

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
