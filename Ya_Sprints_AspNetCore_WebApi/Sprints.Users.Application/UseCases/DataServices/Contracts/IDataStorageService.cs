using Sprints.Users.Application.Dtos.EntitiesDtos;
using Sprints.Users.Application.Abstractions; 
using Sprints.Users.Application.Internal;
using Sprints.Users.Application.Dtos;
using Sprints.Users.Domain.Entities;


namespace Sprints.Users.Application.UseCases.DataServices.Contracts;

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