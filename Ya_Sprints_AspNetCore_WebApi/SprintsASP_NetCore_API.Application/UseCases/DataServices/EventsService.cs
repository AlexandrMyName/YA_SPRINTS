using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Application.Dtos;
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.Extensions.Logging;
using AutoMapper;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices;


public class EventsService : IEventService
{

    private readonly IInterceptLockings _interceptLockings;
    private readonly IRepository<Event> _repository;
    private readonly ILogger<EventsService> _logger;
    private readonly IMapper _mapper;


    public EventsService(
        IRepository<Event> repository,
        ILogger<EventsService> logger,
        IInterceptLockings interceptLockings,
        IMapper mapper)
    {
        _repository = repository;
        _interceptLockings = interceptLockings;
        _logger = logger;
        _mapper = mapper;
    }

    // Комплексная логика  

    public async Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(eventId);
        await semaphore.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            var eventResult = await _repository.GetByIdAsync(eventId);
            if (!eventResult.IsSuccesfuly || eventResult.Data == null)
                throw new NullReferenceException("Event is null");

            eventResult.Data.ReleaseSeats(count);
            await _repository.UpdateAsync(eventResult.Data);
            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Одиночные операции  

    public async Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(dto.Id);
        await semaphore.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Запрос добавления события с ID: " + dto.Id);

            var eventEntity = Event.Create(
                dto.Id, dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats ?? 0);

            var result = await _repository.AddAsync(eventEntity);
            if (!result.IsSuccesfuly)
                return ResultDto<IEventInfoDto>.Fail(result?.Reason ?? "");

            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Добавлена модель: {eventEntity?.Id}");
            return ResultDto<IEventInfoDto>.Ok(
                _mapper.Map<EventInfoDto>(eventEntity), result?.Message ?? "");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item)
    {
        var result = await UpdateAsync(item);
        if (result.IsSuccesfuly && result.Data != null)
        {
            _logger.LogInformation($"Обновлена модель: {result.Data.Title}");
            return ResultDto<IEventInfoDto>.Ok(result.Data, result.Message ?? "");
        }
        return ResultDto<IEventInfoDto>.Fail(result.Reason ?? "");
    }

    // CRUD (реализация IDataStorageService)  

    public async Task<IResultDto<IEventInfoDto>> AddAsync(IEventInfoDto item)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(item.Id);
        await semaphore.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Запрос добавления события с ID: " + item.Id);

            var eventEntity = await _repository.AddAsync(_mapper.Map<Event>(item));
            if (!eventEntity.IsSuccesfuly)
                return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Добавлена модель: {eventEntity?.Data?.Title}");
            return ResultDto<IEventInfoDto>.Ok(item, eventEntity?.Message ?? "");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IResultDto<IEventInfoDto>> UpdateAsync(IEventInfoDto item)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(item.Id);
        await semaphore.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Запрос обновления события с ID: " + item.Id);

            var eventEntity = await _repository.UpdateAsync(_mapper.Map<Event>(item));
            if (!eventEntity.IsSuccesfuly)
                return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Обновлена модель: {eventEntity?.Data?.Title}");
            return ResultDto<IEventInfoDto>.Ok(item, eventEntity?.Message ?? "");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IResultDto<IEventInfoDto>> DeleteAsync(Guid id)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(id);
        await semaphore.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Запрос удаления события с ID: " + id);

            var eventEntity = await _repository.DeleteAsync(id);
            if (!eventEntity.IsSuccesfuly)
                return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"Удалена модель c ID: {id}");
            return ResultDto<IEventInfoDto>.Ok(eventEntity?.Message ?? "");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Массовые операции  

    public async Task<IResultDto<IEventInfoDto>> AddRangeAsync(IEnumerable<IEventInfoDto> items)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            return ResultDto<IEventInfoDto>.Fail("Список пуст");

        var ids = itemList.Select(i => i.Id).OrderBy(id => id).ToList();
        var semaphores = ids.Select(id => _interceptLockings.GetOrAddByEventId(id)).ToList();

        foreach (var sem in semaphores)
            await sem.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Запрос добавления списка событий в коллекцию");

            var result = await _repository.AddRangeAsync(
                itemList.Select(i => _mapper.Map<Event>(i)).ToList());

            if (!result.IsSuccesfuly)
                return ResultDto<IEventInfoDto>.Fail(result?.Reason ?? "");

            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Добавление моделей данных - успешно");
            return ResultDto<IEventInfoDto>.Ok(result?.Message ?? "");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            foreach (var sem in semaphores.AsEnumerable().Reverse())
                sem.Release();
        }
    }

    public async Task<IResultDto<IEventInfoDto>> UpdateRangeAsync(IEnumerable<IEventInfoDto> items)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            return ResultDto<IEventInfoDto>.Fail("Список пуст");

        var ids = itemList.Select(i => i.Id).OrderBy(id => id).ToList();
        var semaphores = ids.Select(id => _interceptLockings.GetOrAddByEventId(id)).ToList();

        foreach (var sem in semaphores)
            await sem.WaitAsync();

        await using var transaction = await _repository.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Запрос обновления списка событий в коллекции");

            var result = await _repository.UpdateRangeAsync(
                itemList.Select(i => _mapper.Map<Event>(i)).ToList());

            if (!result.IsSuccesfuly)
                return ResultDto<IEventInfoDto>.Fail(result?.Reason ?? "");

            await _repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Обновление моделей данных - успешно");
            return ResultDto<IEventInfoDto>.Ok(result?.Message ?? "");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            foreach (var sem in semaphores.AsEnumerable().Reverse())
                sem.Release();
        }
    }

    // Чтение  

    public async Task<IEnumerable<IEventInfoDto>> GetAllAsync()
    {
        _logger.LogInformation("Запрос всех событий");
        var events = await _repository.GetAllAsync();
        _logger.LogInformation($"Количество: {events.Count()} данных");
        return events.Select(e => _mapper.Map<IEventInfoDto>(e)).ToList();
    }

    public async Task<PaginatedResult<IEventInfoDto>> GetFilteredAsync(IEntityFilter<Event> filter)
    {
        _logger.LogInformation("Запрос с фильтрацией и пагинацией для {Entity}", typeof(Event).Name);

        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 20;

        var paged = await _repository.GetPagedAsync(
            filter.ToPredicate(), page, pageSize, default);

        var dtos = paged.Items.Select(e => _mapper.Map<IEventInfoDto>(e)).ToList();

        _logger.LogInformation($"Возвращено {dtos.Count} элементов из {paged.TotalCount}");
        return PaginatedResult<IEventInfoDto>.Create(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }

    public async Task<IResultDto<IEventInfoDto>> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Запрос события с ID: " + id);

        var eventEntity = await _repository.GetByIdAsync(id);
        if (eventEntity.IsSuccesfuly && eventEntity.Data != null)
        {
            _logger.LogInformation($"Получена модель {eventEntity.Data.Title}");
            return ResultDto<IEventInfoDto>.Ok(
                _mapper.Map<IEventInfoDto>(eventEntity.Data), eventEntity.Message ?? "");
        }
        return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");
    }

    public bool IsExisted(Guid id) => _repository.IsExisted(id);
    public bool IsExistedByTitle(string name) => _repository.IsExistedByTitle(name);
}