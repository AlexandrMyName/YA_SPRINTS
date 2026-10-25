using Sprints.Events.Application.UseCases.DataServices.Contracts;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.Abstractions;
using Sprints.Events.Application.Internal;
using Sprints.Events.Domain.Exceptions;
using Sprints.Events.Application.Dtos;
using Sprints.Events.Domain.Entities;
using Microsoft.Extensions.Logging;
using AutoMapper;


namespace Sprints.Events.Application.UseCases.DataServices;


public class EventsService : IEventService
{

    private readonly IInterceptLockings _interceptLockings;
    private readonly IRepository<Event> _repository;
    private readonly ILogger<EventsService> _logger;
    private readonly IMapper _mapper;
    private readonly IEventCache _cache;


    public EventsService(
        IRepository<Event> repository,
        IEventCache cache,
        ILogger<EventsService> logger,
        IInterceptLockings interceptLockings,
        IMapper mapper)
    {
        _repository = repository;
        _interceptLockings = interceptLockings;
        _logger = logger;
        _mapper = mapper;
        _cache = cache;
    }


    public bool IsExisted(Guid id) => _repository.IsExisted(id);
    public bool IsExistedByTitle(string name) => _repository.IsExistedByTitle(name);

    /// <summary>
    /// Inbox: ReleaseSeats (из Kafka).
    /// Кеш единичного события инвалидируется; top-10 не трогаем — TTL 60 c.
    /// </summary>
    public async Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count)
    {

        var semaphore = _interceptLockings.GetOrAddByEventId(eventId);
        await semaphore.WaitAsync();

        try
        {
            await using (var transaction = await _repository.BeginTransactionAsync())
            {
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
            }

            // Инвалидация вне транзакции. Если Redis упадёт — ResilientEventCache
            // залогирует warning, а TTL подстрахует от долгого стейла.
            await _cache.InvalidateEventAsync(eventId);

            _logger.LogInformation("Места освобождены для события {EventId}: -{Count}", eventId, count);
        }
        finally
        {
            semaphore.Release();
        }
    }


    public async Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto)
    {
        if (dto.Id == Guid.Empty || dto.Id == default)
            dto.Id = Guid.NewGuid();

        if (IsExisted(dto.Id) || IsExistedByTitle(dto.Title))
            throw new DuplicateEventException(
                $"Событие с Id={dto.Id} или Title='{dto.Title}' уже существует");

        var semaphore = _interceptLockings.GetOrAddByEventId(dto.Id);
        await semaphore.WaitAsync();

        try
        {
            IResultDto<IEventInfoDto> result;

            await using (var transaction = await _repository.BeginTransactionAsync())
            {
                try
                {
                    _logger.LogInformation("Запрос добавления события с ID: {EventId}", dto.Id);

                    var eventEntity = Event.Create(
                        dto.Id, dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats ?? 0);

                    var addResult = await _repository.AddAsync(eventEntity);
                    if (!addResult.IsSuccesfuly)
                        return ResultDto<IEventInfoDto>.Fail(addResult?.Reason ?? "");

                    await _repository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result = ResultDto<IEventInfoDto>.Ok(
                        _mapper.Map<EventInfoDto>(eventEntity), addResult?.Message ?? "");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            // Новое событие может попасть в top-10 (если событий меньше 10) → инвалидируем. .
            await _cache.InvalidateTopEventsAsync();

            _logger.LogInformation("Добавлена модель: {EventId}", dto.Id);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }


    public async Task<IResultDto<IEventInfoDto>> CreateEventsRangeAsync(IEnumerable<ICreateEventDto> dtos)
    {

        var dtoList = dtos.ToList();
        if (!dtoList.Any())
            return ResultDto<IEventInfoDto>.Fail("Список пуст");

        var prepared = new List<EventInfoDto>(dtoList.Count);

        foreach (var d in dtoList)
        {
            if (d.Id == Guid.Empty || d.Id == default)
                d.Id = Guid.NewGuid();

            while (IsExisted(d.Id) || prepared.Any(p => p.Id == d.Id))
                d.Id = Guid.NewGuid();

            if (IsExistedByTitle(d.Title))
                throw new DuplicateEventException(
                    $"Событие с названием '{d.Title}' уже существует");

            prepared.Add(_mapper.Map<EventInfoDto>(d));
        }

        return await AddRangeAsync(prepared);
    }
 

    public async Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item)
    {
        var result = await UpdateAsync(item);
        if (result.IsSuccesfuly && result.Data != null)
        {
            _logger.LogInformation("Обновлена модель: {Title}", result.Data.Title);
            return ResultDto<IEventInfoDto>.Ok(result.Data, result.Message ?? "");
        }
        return ResultDto<IEventInfoDto>.Fail(result.Reason ?? "");
    }


    public async Task<IResultDto<IEventInfoDto>> AddAsync(IEventInfoDto item)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(item.Id);
        await semaphore.WaitAsync();

        try
        {
            IResultDto<IEventInfoDto> result;

            await using (var transaction = await _repository.BeginTransactionAsync())
            {
                try
                {
                    _logger.LogInformation("Запрос добавления события с ID: {EventId}", item.Id);

                    var eventEntity = await _repository.AddAsync(_mapper.Map<Event>(item));
                    if (!eventEntity.IsSuccesfuly)
                        return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

                    await _repository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result = ResultDto<IEventInfoDto>.Ok(item, eventEntity?.Message ?? "");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            await _cache.InvalidateTopEventsAsync();

            _logger.LogInformation("Добавлена модель: {Title}", item.Title);
            return result;
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

        try
        {
            IResultDto<IEventInfoDto> result;

            await using (var transaction = await _repository.BeginTransactionAsync())
            {
                try
                {
                    _logger.LogInformation("Запрос обновления события с ID: {EventId}", item.Id);

                    var eventEntity = await _repository.UpdateAsync(_mapper.Map<Event>(item));
                    if (!eventEntity.IsSuccesfuly)
                        return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

                    await _repository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result = ResultDto<IEventInfoDto>.Ok(item, eventEntity?.Message ?? "");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            // Update меняет TotalSeats/Title → популярность (TotalSeats - AvailableSeats)
            // могла измениться → инвалидируем и единичный ключ, и top-10.
            await _cache.InvalidateEventAsync(item.Id);
            await _cache.InvalidateTopEventsAsync();

            _logger.LogInformation("Обновлена модель: {Title}", item.Title);
            return result;
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

        try
        {
            IResultDto<IEventInfoDto> result;

            await using (var transaction = await _repository.BeginTransactionAsync())
            {
                try
                {
                    _logger.LogInformation("Запрос удаления события с ID: {EventId}", id);

                    var eventEntity = await _repository.DeleteAsync(id);
                    if (!eventEntity.IsSuccesfuly)
                        return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

                    await _repository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result = ResultDto<IEventInfoDto>.Ok(eventEntity?.Message ?? "");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            await _cache.InvalidateEventAsync(id);
            // Удалённое событие не должно отображаться в top-10 до истечения TTL.
            await _cache.InvalidateTopEventsAsync();

            _logger.LogInformation("Удалена модель c ID: {EventId}", id);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }


    public async Task<IResultDto<IEventInfoDto>> AddRangeAsync(IEnumerable<IEventInfoDto> items)
    {
        var itemList = items.ToList();
        if (!itemList.Any())
            return ResultDto<IEventInfoDto>.Fail("Список пуст");

        var ids = itemList.Select(i => i.Id).OrderBy(id => id).ToList();
        var semaphores = ids.Select(id => _interceptLockings.GetOrAddByEventId(id)).ToList();

        foreach (var sem in semaphores)
            await sem.WaitAsync();

        try
        {
            IResultDto<IEventInfoDto> result;

            await using (var transaction = await _repository.BeginTransactionAsync())
            {
                try
                {
                    _logger.LogInformation("Запрос добавления списка событий в коллекцию");

                    var addResult = await _repository.AddRangeAsync(
                        itemList.Select(i => _mapper.Map<Event>(i)).ToList());

                    if (!addResult.IsSuccesfuly)
                        return ResultDto<IEventInfoDto>.Fail(addResult?.Reason ?? "");

                    await _repository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result = ResultDto<IEventInfoDto>.Ok(addResult?.Message ?? "");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            await _cache.InvalidateTopEventsAsync();

            _logger.LogInformation("Добавление моделей данных - успешно");
            return result;
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

        var missing = itemList.Where(i => !IsExisted(i.Id)).Select(i => $"{i.Id}:{i.Title}").ToList();
        if (missing.Count > 0)
            return ResultDto<IEventInfoDto>.Fail(
                "Не существуют указанные сущности: " + string.Join(", ", missing));

        var ids = itemList.Select(i => i.Id).OrderBy(id => id).ToList();
        var semaphores = ids.Select(id => _interceptLockings.GetOrAddByEventId(id)).ToList();

        foreach (var sem in semaphores)
            await sem.WaitAsync();

        try
        {
            IResultDto<IEventInfoDto> result;

            await using (var transaction = await _repository.BeginTransactionAsync())
            {
                try
                {
                    _logger.LogInformation("Запрос обновления списка событий в коллекции");

                    var updateResult = await _repository.UpdateRangeAsync(
                        itemList.Select(i => _mapper.Map<Event>(i)).ToList());

                    if (!updateResult.IsSuccesfuly)
                        return ResultDto<IEventInfoDto>.Fail(updateResult?.Reason ?? "");

                    await _repository.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result = ResultDto<IEventInfoDto>.Ok(updateResult?.Message ?? "");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            foreach (var id in ids)
                await _cache.InvalidateEventAsync(id);

            await _cache.InvalidateTopEventsAsync();

            _logger.LogInformation("Обновление моделей данных - успешно");
            return result;
        }
        finally
        {
            foreach (var sem in semaphores.AsEnumerable().Reverse())
                sem.Release();
        }
    }


    public async Task<IEnumerable<IEventInfoDto>> GetAllAsync()
    {
        _logger.LogInformation("Запрос всех событий");
        var events = await _repository.GetAllAsync();
        _logger.LogInformation("Количество: {Count} данных", events.Count());
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

        _logger.LogInformation("Возвращено {Count} элементов из {Total}", dtos.Count, paged.TotalCount);
        return PaginatedResult<IEventInfoDto>.Create(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }

    /// <summary>
    /// GetById с Cache-Aside
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IResultDto<IEventInfoDto>> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Запрос события с ID: {EventId}", id);

        var cached = await _cache.GetEventAsync(id);
        if (cached is not null)
        {
            _logger.LogInformation("Событие {EventId} из кеша", id);
            return ResultDto<IEventInfoDto>.Ok(cached, "Из кеша");
        }

        // Cache miss → идём в БД
        var eventEntity = await _repository.GetByIdAsync(id);
        if (!eventEntity.IsSuccesfuly || eventEntity.Data is null)
            return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? "");

        var dto = _mapper.Map<EventInfoDto>(eventEntity.Data);
        await _cache.SetEventAsync(dto);

        _logger.LogInformation("Получена модель {Title} из БД", eventEntity.Data.Title);
        return ResultDto<IEventInfoDto>.Ok(dto, eventEntity.Message ?? "");
    }


    public async Task<IReadOnlyList<IEventInfoDto>> GetTopAsync(int take = 10, CancellationToken ct = default)
    {
        // Ключ events:top10 — всегда про 10. Если клиент просит меньше - режем на выдаче,
        // но кешируем/читаем из БД всегда 10. Это исключает cache poisoning по параметру take.
        const int cacheSize = 10;
        if (take > cacheSize) take = cacheSize;

        _logger.LogInformation("Запрос топа событий (take={Take})", take);

        var cached = await _cache.GetTopEventsAsync(ct);
        if (cached is not null)
        {
            _logger.LogInformation("Топ событий из кеша: {Count}", cached.Count);
            return cached.Take(take).Cast<IEventInfoDto>().ToList();
        }

        // Популярность = занятые места = TotalSeats - AvailableSeats
        var entities = await _repository.GetOrderedAsync(
            q => q.OrderByDescending(e => e.TotalSeats - e.AvailableSeats),
            cacheSize,
            ct);

        var dtos = _mapper.Map<List<EventInfoDto>>(entities);
        await _cache.SetTopEventsAsync(dtos, ct);

        _logger.LogInformation("Топ событий из БД: {Count}", dtos.Count);
        return dtos.Take(take).Cast<IEventInfoDto>().ToList();
    }
}