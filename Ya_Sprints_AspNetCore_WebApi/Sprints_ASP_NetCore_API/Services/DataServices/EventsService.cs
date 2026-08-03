using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;
using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Filters.ActionFilters;
using SprintASP_NetCore_API.Services;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using static System.Net.WebRequestMethods;


namespace Sprints_Project_ASP_NetCore_API.Services.DataServices
{
    
    public class EventsService : IEventService
    {

        public EventsService(
            IServiceScopeFactory scopeFactory,
            IRepository<IEvent> repository, 
            ILogger<EventsService> logger, 
            IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _repository = repository;
            _logger = logger;
            _mapper = mapper;
        }
         
        private readonly IServiceScopeFactory _scopeFactory; // на будущее когда БД появится мб. переместить лучше в репозиторий
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _eventLocks = new(); // симофоры для событий 
        private readonly IRepository<IEvent> _repository;
        private readonly ILogger<EventsService> _logger;
        private readonly IMapper _mapper;
         
         
        public async Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count)
        {

            var semaphore = _eventLocks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            try
            {
                var eventResult = await _repository.GetByIdAsync(eventId);
                if (!eventResult.IsSuccesfuly || eventResult.Data == null) throw new NullReferenceException("Event is null");
                eventResult.Data.ReleaseSeats(count);
                await _repository.UpdateAsync(eventResult.Data);
            }
            finally
            {
                semaphore.Release();
            }
        }
         

         
        public async Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto)
        {
            // Логика валидации входящего totalSeat в ValidateInputModelAttribute (Action Filter)  + вместо создания через фабричный метод - AutoMap (Но фабричный метод так же работет) 
            // 6. Убедитесь, что Event.Create(...) теперь принимает totalSeats и валидирует его: значение должно быть больше нуля.При нарушении — ValidationException.
            // 7. Обновите EventService.CreateEventAsync — передавайте TotalSeats в фабричный метод и возвращайте оба новых поля в EventInfo. 
            _logger.LogInformation("Запрос добавления события с ID: " + dto.Id);
            var eventEntity = await _repository.AddAsync(_mapper.Map<Event>(dto)); 
            // _repository.AddAsync(Event.Create(dto.Title, dto.Description, dto.StartAt, dto.EndAt, dto.TotalSeats ?? 0));  Добавил на всякий случай. Логика внутри валидируется (через фабричный метод)

            if (eventEntity.IsSuccesfuly){
                _logger.LogInformation($"Добавлена модель: {eventEntity?.Data?.Id}");
                return ResultDto<IEventInfoDto>.Ok(_mapper.Map<EventInfoDto>(eventEntity.Data), eventEntity?.Message ?? "");
            } 
            return ResultDto<IEventInfoDto>.Fail(eventEntity?.Reason ?? ""); 
        }

        public Task<PaginatedResult<IBookingInfoDto>> GetFilteredEventsAsync(IEntityFilter<IEntity> filter)
        {
            throw new NotImplementedException();
        }

        public Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item)
        {
            throw new NotImplementedException();
        }



        public async Task<IEnumerable<EventInfoDto>> GetAllAsync()
        {
            _logger.LogInformation("Запрос всех событий");
            var events = await _repository.GetAllAsync();
            _logger.LogInformation($"Количество: {events.Count()} данных");  
            // действие с Entity (на будущее)
            return events.Select(e=>_mapper.Map<EventInfoDto>(e)).ToList();
        }

        public async Task<PaginatedResult<EventInfoDto>> GetFilteredAsync(IEntityFilter<IEntity> filter)
        {

            var filterEvents = filter as EventFilterDto;

            _logger.LogInformation("Запрос с фильтрацией и пагинацией для {Entity}", filterEvents.GetType().Name);

            // Получаем IQueryable
            var query = await _repository.GetQueryAsync();

            // Применяем фильтрацию 
            var filteredQuery = filter.Apply(query) as IQueryable<IEvent>;

            if (filteredQuery == null) throw new InvalidOperationException("Не удалось преминить фильтр");
            
            // Получаем общее количество (ДО пагинации!)
            var totalCount = filteredQuery.Count();

            // Применяем пагинацию отдельно
            if (filterEvents.Page.HasValue && filterEvents.PageSize.HasValue)
            {
                filteredQuery = filteredQuery
                    .Skip((filterEvents.Page.Value - 1) * filterEvents.PageSize.Value)
                    .Take(filterEvents.PageSize.Value);
            }

            // Получаем данные
            var items = filteredQuery.ToList();

            // Маппим в DTO
            var dtos = items.Select(e => _mapper.Map<EventInfoDto>(e)).ToList();

            _logger.LogInformation($"Возвращено {dtos.Count} элементов из {totalCount}");

            return PaginatedResult<EventInfoDto>.Create(
                dtos,
                totalCount,
                filterEvents.Page ?? 1,
                filterEvents.PageSize ?? totalCount
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

            return ResultDto<EventInfoDto>.Fail(  eventEntity?.Reason ?? "");  
        }

        public async Task<IResultDto<EventInfoDto>> AddAsync(EventInfoDto item)
        {

            _logger.LogInformation("Запрос добавления события с ID: " + item.Id);
            var eventEntity = await _repository.AddAsync(_mapper.Map<Event>(item));
         
            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Добавлена модель: {eventEntity?.Data?.Title}");
                return ResultDto<EventInfoDto>.Ok(item, eventEntity?.Message ?? "");
            }

            return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
        } 

        public async Task<IResultDto<EventInfoDto>> UpdateAsync(EventInfoDto item)
        {

            _logger.LogInformation("Запрос обновления события с ID: " + item.Id);
            var eventEntity = await _repository.UpdateAsync(_mapper.Map<Event>(item));

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Обновлена модель: {eventEntity?.Data?.Title}");
                return ResultDto<EventInfoDto>.Ok(item, eventEntity?.Message ?? "");
            }

            return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
        }

        public async Task<IResultDto<EventInfoDto>> DeleteAsync(Guid id)
        {

            _logger.LogInformation("Запрос удаления события с ID: " + id);
            var eventEntity = await _repository.DeleteAsync(id);

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Удалена модель c ID: {id}");
                return ResultDto<EventInfoDto>.Ok(eventEntity?.Message ?? "");
            } 
            return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
        }
        

        public async Task<IResultDto<EventInfoDto>> AddRangeAsync(IEnumerable<EventInfoDto> items)
        {

            _logger.LogInformation("Запрос добавления списка событий в коллекцию"); 
            var eventEntity = await _repository.AddRangeAsync(items.Select(i=> _mapper.Map<Event>(i)).ToList());

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Добавление моделей данных - успешно");
                return ResultDto<EventInfoDto>.Ok(eventEntity?.Message ?? "");
            } 
            return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
        }


        public async Task<IResultDto<EventInfoDto>> UpdateRangeAsync(IEnumerable<EventInfoDto> items)
        {

            _logger.LogInformation("Запрос обновления списка событий в коллекции");
            var eventEntity = await _repository.UpdateRangeAsync(items.Select(i => _mapper.Map<Event>(i)).ToList());

            if (eventEntity.IsSuccesfuly){
                _logger.LogInformation($"Обновление моделей данных - успешно");
                return ResultDto<EventInfoDto>.Ok(eventEntity?.Message ?? "");
            }
            return ResultDto<EventInfoDto>.Fail(eventEntity?.Reason ?? "");
        }


        public bool IsExisted(Guid id) => _repository.IsExisted(id);

        public bool IsExistedByTitle(string name) => _repository.IsExistedByTitle(name);

       
    }
}
