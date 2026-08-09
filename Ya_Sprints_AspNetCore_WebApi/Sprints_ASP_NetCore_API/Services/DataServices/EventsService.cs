using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services.Intercepts;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;

namespace Sprints_Project_ASP_NetCore_API.Services.DataServices
{
    public class EventsService : IEventService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IInterceptLockings _interceptLockings;
        private readonly IRepository<Event> _repository;
        private readonly ILogger<EventsService> _logger;
        private readonly IMapper _mapper;

        public EventsService(
            IServiceScopeFactory scopeFactory,
            IRepository<Event> repository,
            ILogger<EventsService> logger,
            IInterceptLockings interceptLockings,
            IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _repository = repository;
            _interceptLockings = interceptLockings;
            _logger = logger;
            _mapper = mapper;
        }

        // ===================== Метод с комплексной логикой =====================

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

        // ===================== Одиночные операции =====================

        public async Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto)
        {
            var semaphore = _interceptLockings.GetOrAddByEventId(dto.Id);
            await semaphore.WaitAsync();

            await using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Запрос добавления события с ID: " + dto.Id);

                var eventEntity = Event.Create(dto.Id, dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats ?? 0);
                var result = await _repository.AddAsync(eventEntity);
                if (!result.IsSuccesfuly)
                {
                    return ResultDto<IEventInfoDto>.Fail(result?.Reason ?? "");
                }

                await _repository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Добавлена модель: {eventEntity?.Id}");
                return ResultDto<IEventInfoDto>.Ok(_mapper.Map<EventInfoDto>(eventEntity), result?.Message ?? "");
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
             
            var result = await UpdateAsync(_mapper.Map<EventInfoDto>(item));
            if (result != null && result.IsSuccesfuly && result.Data != null)
            {
                _logger.LogInformation($"Обновлена модель: {result?.Data?.Title}");
                return ResultDto<IEventInfoDto>.Ok(result!.Data, result?.Message ?? "");
            }
            return ResultDto<IEventInfoDto>.Fail(result?.Reason ?? "");
        }


        public async Task<IResultDto<EventInfoDto>> AddAsync(EventInfoDto item)
        {
            var semaphore = _interceptLockings.GetOrAddByEventId(item.Id);
            await semaphore.WaitAsync();

            await using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Запрос добавления события с ID: " + item.Id);
                var eventEntity = await _repository.AddAsync(_mapper.Map<Event>(item));
                if (!eventEntity.IsSuccesfuly)
                {
                    return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
                }

                await _repository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Добавлена модель: {eventEntity?.Data?.Title}");
                return ResultDto<EventInfoDto>.Ok(item, eventEntity?.Message ?? "");
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

        public async Task<IResultDto<EventInfoDto>> UpdateAsync(EventInfoDto item)
        {
            var semaphore = _interceptLockings.GetOrAddByEventId(item.Id);
            await semaphore.WaitAsync();

            await using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Запрос обновления события с ID: " + item.Id);
                var eventEntity = await _repository.UpdateAsync(_mapper.Map<Event>(item));
                if (!eventEntity.IsSuccesfuly)
                {
                    return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
                }

                await _repository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Обновлена модель: {eventEntity?.Data?.Title}");
                return ResultDto<EventInfoDto>.Ok(item, eventEntity?.Message ?? "");
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

        public async Task<IResultDto<EventInfoDto>> DeleteAsync(Guid id)
        {
            var semaphore = _interceptLockings.GetOrAddByEventId(id);
            await semaphore.WaitAsync();

            await using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Запрос удаления события с ID: " + id);
                var eventEntity = await _repository.DeleteAsync(id);
                if (!eventEntity.IsSuccesfuly)
                {
                    return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
                }

                await _repository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Удалена модель c ID: {id}");
                return ResultDto<EventInfoDto>.Ok(eventEntity?.Message ?? "");
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

        // ===================== Массовые операции =====================

        public async Task<IResultDto<EventInfoDto>> AddRangeAsync(IEnumerable<EventInfoDto> items)
        {
            var itemList = items.ToList();
            if (!itemList.Any())
                return ResultDto<EventInfoDto>.Fail("Список пуст");

            // Сортируем ID для предотвращения дедлоков
            var ids = itemList.Select(i => i.Id).OrderBy(id => id).ToList();
            var semaphores = ids.Select(id => _interceptLockings.GetOrAddByEventId(id)).ToList();

            foreach (var sem in semaphores)
                await sem.WaitAsync();

            await using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Запрос добавления списка событий в коллекцию");
                var result = await _repository.AddRangeAsync(itemList.Select(i => _mapper.Map<Event>(i)).ToList());
                if (!result.IsSuccesfuly)
                {
                    return ResultDto<EventInfoDto>.Fail(result?.Reason ?? "");
                }

                await _repository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Добавление моделей данных - успешно");
                return ResultDto<EventInfoDto>.Ok(result?.Message ?? "");
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

        public async Task<IResultDto<EventInfoDto>> UpdateRangeAsync(IEnumerable<EventInfoDto> items)
        {
            var itemList = items.ToList();
            if (!itemList.Any())
                return ResultDto<EventInfoDto>.Fail("Список пуст");

            // Сортируем ID для предотвращения дедлоков
            var ids = itemList.Select(i => i.Id).OrderBy(id => id).ToList();
            var semaphores = ids.Select(id => _interceptLockings.GetOrAddByEventId(id)).ToList();

            foreach (var sem in semaphores)
                await sem.WaitAsync();

            await using var transaction = await _repository.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Запрос обновления списка событий в коллекции");
                var result = await _repository.UpdateRangeAsync(itemList.Select(i => _mapper.Map<Event>(i)).ToList());
                if (!result.IsSuccesfuly)
                {
                    return ResultDto<EventInfoDto>.Fail(result?.Reason ?? "");
                }

                await _repository.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Обновление моделей данных - успешно");
                return ResultDto<EventInfoDto>.Ok(result?.Message ?? "");
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

        // ===================== Чтение (без семафоров) =====================

        public async Task<IEnumerable<EventInfoDto>> GetAllAsync()
        {
            _logger.LogInformation("Запрос всех событий");
            var events = await _repository.GetAllAsync();
            _logger.LogInformation($"Количество: {events.Count()} данных");
            return events.Select(e => _mapper.Map<EventInfoDto>(e)).ToList();
        }

        public async Task<PaginatedResult<EventInfoDto>> GetFilteredAsync(IEntityFilter<IEntity> filter)
        {
            var filterEvents = filter as EventFilterDto;
            _logger.LogInformation("Запрос с фильтрацией и пагинацией для {Entity}", filterEvents?.GetType().Name);

            var query = await _repository.GetQueryAsync();
            var filteredQuery = filter.Apply(query) as IQueryable<IEvent>;

            if (filteredQuery == null)
                throw new InvalidOperationException("Не удалось применить фильтр");

            var totalCount = filteredQuery.Count();

            if (filterEvents?.Page.HasValue == true && filterEvents.PageSize.HasValue)
            {
                filteredQuery = filteredQuery
                    .Skip((filterEvents.Page.Value - 1) * filterEvents.PageSize.Value)
                    .Take(filterEvents.PageSize.Value);
            }

            var items = filteredQuery.ToList();
            var dtos = items.Select(e => _mapper.Map<EventInfoDto>(e)).ToList();

            _logger.LogInformation($"Возвращено {dtos.Count} элементов из {totalCount}");
            return PaginatedResult<EventInfoDto>.Create(
                dtos,
                totalCount,
                filterEvents?.Page ?? 1,
                filterEvents?.PageSize ?? totalCount
            );
        }

        public async Task<IResultDto<EventInfoDto>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Запрос события с ID: " + id);
            var eventEntity = await _repository.GetByIdAsync(id);
            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Получена модель {eventEntity?.Data?.Title}");
                return ResultDto<EventInfoDto>.Ok(_mapper.Map<EventInfoDto>((Event)eventEntity?.Data), eventEntity?.Message ?? "");
            }
            return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
        }

        public bool IsExisted(Guid id) => _repository.IsExisted(id);
        public bool IsExistedByTitle(string name) => _repository.IsExistedByTitle(name);
    }
}