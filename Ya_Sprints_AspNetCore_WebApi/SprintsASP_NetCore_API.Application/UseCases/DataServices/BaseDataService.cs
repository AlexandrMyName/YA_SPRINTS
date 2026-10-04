using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts; 
using Sprints_Project_ASP_NetCore_API.Application.Dtos.EntitiesDtos; 
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Application.Dtos;
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.Extensions.Logging;
using AutoMapper;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices;


public class BaseDataService<TDto, TEntity> : IDataStorageService<TDto, TEntity>
    where TDto : class, IEntityDto
    where TEntity : class, IEntity
{

    protected readonly IRepository<TEntity> Repository;
    protected readonly ILogger<BaseDataService<TDto, TEntity>> Logger;
    protected readonly IMapper Mapper;

    public BaseDataService(
        IRepository<TEntity> repository,
        ILogger<BaseDataService<TDto, TEntity>> logger,
        IMapper mapper)
    {
        Repository = repository;
        Logger = logger;
        Mapper = mapper;
    }

    public async Task<IEnumerable<TDto>> GetAllAsync()
    {
        Logger.LogDebug("Запрос данных (GetAll)");
        var datas = await Repository.GetAllAsync();
        Logger.LogDebug($"Количество: {datas.Count()} данных");
        return datas.Select(e => Mapper.Map<TDto>(e)).ToList();
    }

    public async Task<PaginatedResult<TDto>> GetFilteredAsync(IEntityFilter<TEntity> filter)
    {
        Logger.LogDebug("Запрос с фильтрацией и пагинацией для {Entity}", typeof(TEntity).Name);

        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var paged = await Repository.GetPagedAsync(
            filter.ToPredicate(), page, pageSize, CancellationToken.None);

        var dtos = paged.Items.Select(e => Mapper.Map<TDto>(e)).ToList();

        Logger.LogDebug($"Возвращено {dtos.Count} элементов из {paged.TotalCount}");
        return PaginatedResult<TDto>.Create(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<IResultDto<TDto>> GetByIdAsync(Guid id)
    {
        Logger.LogDebug("Запрос по ID: " + id);
        var entity = await Repository.GetByIdAsync(id);

        if (entity.IsSuccesfuly)
        {
            Logger.LogDebug($"Получена модель {entity?.Data?.Id}");
            var dto = Mapper.Map<TDto>(entity!.Data);
            return ResultDto<TDto>.Ok(dto, entity.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> AddAsync(TDto item)
    {
        Logger.LogDebug("Запрос добавления Entity с ID: " + item.Id);
        var entity = await Repository.AddAsync(Mapper.Map<TEntity>(item));

        if (entity.IsSuccesfuly)
        {
            Logger.LogDebug($"Добавлена модель c ID: {entity?.Data?.Id}");
            return ResultDto<TDto>.Ok(item, entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> UpdateAsync(TDto item)
    {
        Logger.LogDebug("Запрос обновления Entity с ID: " + item.Id);
        var entity = await Repository.UpdateAsync(Mapper.Map<TEntity>(item));

        if (entity.IsSuccesfuly)
        {
            Logger.LogDebug($"Обновлена модель c ID: {entity?.Data?.Id}");
            return ResultDto<TDto>.Ok(item, entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> DeleteAsync(Guid id)
    {
        Logger.LogDebug("Запрос удаления Entity с ID: " + id);
        var entity = await Repository.DeleteAsync(id);

        if (entity.IsSuccesfuly)
        {
            Logger.LogDebug($"Удалена модель c ID: {id}");
            return ResultDto<TDto>.Ok(entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> AddRangeAsync(IEnumerable<TDto> items)
    {
        Logger.LogDebug("Запрос добавления списка Entity в коллекцию");
        var entity = await Repository.AddRangeAsync(
            items.Select(i => Mapper.Map<TEntity>(i)).ToList());

        if (entity.IsSuccesfuly)
        {
            Logger.LogDebug("Добавление моделей данных - успешно");
            return ResultDto<TDto>.Ok(entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> UpdateRangeAsync(IEnumerable<TDto> items)
    {
        Logger.LogDebug("Запрос обновления списка Entity в коллекции");
        var entity = await Repository.UpdateRangeAsync(
            items.Select(i => Mapper.Map<TEntity>(i)).ToList());

        if (entity.IsSuccesfuly)
        {
            Logger.LogDebug("Обновление моделей данных - успешно");
            return ResultDto<TDto>.Ok(entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public bool IsExisted(Guid id) => Repository.IsExisted(id);

    public bool IsExistedByTitle(string name)
        => throw new NotSupportedException("Проверка по названию не поддерживается");
}