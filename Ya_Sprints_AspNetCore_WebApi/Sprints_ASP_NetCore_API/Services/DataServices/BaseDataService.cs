using AutoMapper;
using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using Sprints_Project_ASP_NetCore_API.Services;

namespace SprintASP_NetCore_API.Services.DataServices;

public class BaseDataService<TDto, TEntity> : IDataStorageService<TDto> where TDto : class, IEntityDto where TEntity : class, IEntity
{

    public BaseDataService( 
       IRepository<TEntity> repository,
       ILogger<BaseDataService<TDto, TEntity>> logger,
       IMapper mapper)
    { 
        Repository = repository;
        _logger = logger;
        _mapper = mapper;
    }
     
    protected readonly IRepository<TEntity> Repository;
    private readonly ILogger<BaseDataService<TDto, TEntity>> _logger;
    private readonly IMapper _mapper;


     

    public async Task<IEnumerable<TDto>> GetAllAsync()
    {
        _logger.LogDebug("Запрос данных (GetAll)");
        var datas = await Repository.GetAllAsync();
        _logger.LogDebug($"Количество: {datas.Count()} данных");
        return datas.Select(e => _mapper.Map<TDto>(e)).ToList();
    }

    public async Task<PaginatedResult<TDto>> GetFilteredAsync(IEntityFilter<IEntity> filter)
    {

        var filterEntity = filter;

        _logger.LogDebug("Запрос с фильтрацией и пагинацией для {Entity}", filterEntity.GetType().Name);

        // Получаем IQueryable
        var query = await Repository.GetQueryAsync();

        // Применяем фильтрацию 
        var filteredQuery = filter.Apply(query) as IQueryable<IBooking>;

        if (filteredQuery == null) throw new InvalidOperationException("Не удалось преминить фильтр");

        // Получаем общее количество (ДО пагинации!)
        var totalCount = filteredQuery.Count();

        // Применяем пагинацию отдельно
        if (filterEntity.Page.HasValue && filterEntity.PageSize.HasValue)
        {
            filteredQuery = filteredQuery
                .Skip((filterEntity.Page.Value - 1) * filterEntity.PageSize.Value)
                .Take(filterEntity.PageSize.Value);
        }

        // Получаем данные
        var items = filteredQuery.ToList();

        // Маппим в DTO
        var dtos = items.Select(e => _mapper.Map<TDto>(e)).ToList();

        _logger.LogDebug($"Возвращено {dtos.Count} элементов из {totalCount}");

        return PaginatedResult<TDto>.Create(
            dtos,
            totalCount,
            filterEntity.Page ?? 1,
            filterEntity.PageSize ?? totalCount
        );
    }

    public async Task<IResultDto<TDto>> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Запрос по ID: " + id);
        var entity = await Repository.GetByIdAsync(id);

        if (entity.IsSuccesfuly)
        {
            _logger.LogDebug($"Получена модель {entity?.Data?.Id}");
            var dto = _mapper.Map<TDto>(entity.Data);
            return ResultDto<TDto>.Ok(dto, entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> AddAsync(TDto item)
    {

        _logger.LogDebug("Запрос добавления Entity с ID: " + item.Id);
        var entity = await Repository.AddAsync(_mapper.Map<TEntity>(item));

        if (entity.IsSuccesfuly)
        {
            _logger.LogDebug($"Добавлена модель c ID: {entity?.Data?.Id}");
            return ResultDto<TDto>.Ok(item, entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> UpdateAsync(TDto item)
    {

        _logger.LogDebug("Запрос обновления Entity с ID: " + item.Id);
        var entity = await Repository.UpdateAsync(_mapper.Map<TEntity>(item));

        if (entity.IsSuccesfuly)
        {
            _logger.LogDebug($"Обновлена модель c ID: {entity?.Data?.Id}");
            return ResultDto<TDto>.Ok(item, entity?.Message ?? "");
        }

        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> DeleteAsync(Guid id)
    {

        _logger.LogDebug("Запрос удаления Entity с ID: " + id);
        var entity = await Repository.DeleteAsync(id);

        if (entity.IsSuccesfuly)
        {
            _logger.LogDebug($"Удалена модель c ID: {id}");
            return ResultDto<TDto>.Ok(entity?.Message ?? "");
        }
        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> AddRangeAsync(IEnumerable<TDto> items)
    {
        _logger.LogDebug("Запрос добавления списка Entity в коллекцию");
        var entity = await Repository.AddRangeAsync(items.Select(i => _mapper.Map<TEntity>(i)).ToList());

        if (entity.IsSuccesfuly)
        {
            _logger.LogDebug($"Добавление моделей данных - успешно");
            return ResultDto<TDto>.Ok(entity?.Message ?? "");
        }
        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public async Task<IResultDto<TDto>> UpdateRangeAsync(IEnumerable<TDto> items)
    {

        _logger.LogDebug("Запрос обновления списка Entity в коллекции");
        var entity = await Repository.UpdateRangeAsync(items.Select(i => _mapper.Map<TEntity>(i)).ToList());

        if (entity.IsSuccesfuly)
        {
            _logger.LogDebug($"Обновление моделей данных - успешно");
            return ResultDto<TDto>.Ok(entity?.Message ?? "");
        }
        return ResultDto<TDto>.Fail(entity?.Reason ?? "");
    }

    public bool IsExisted(Guid id) => Repository.IsExisted(id);

    public bool IsExistedByTitle(string name) => throw new NotSupportedException("Проверка по названию не поддерживается");
    
     
}