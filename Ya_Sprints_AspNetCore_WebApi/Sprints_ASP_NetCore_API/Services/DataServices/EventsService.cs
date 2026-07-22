using AutoMapper;
using dynamicQueryBuilder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Data.Dtos.Filters; 
using System.Globalization;


namespace Sprints_Project_ASP_NetCore_API.Services.DataServices
{

   

    public class EventsService : IDataStorageService<EventDto>
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
         
        private readonly IRepository<IEvent> _repository;
        private readonly ILogger<EventsService> _logger;
        private readonly IMapper _mapper;

        
        public async Task<IEnumerable<EventDto>> GetAllAsync()
        {
            _logger.LogInformation("Запрос всех событий");
            var events = await _repository.GetAllAsync();
            _logger.LogInformation($"Количество: {events.Count()} данных");  
            // действие с Entity (на будущее)
            return events.Select(e=>_mapper.Map<EventDto>(e)).ToList();
        }


        public async Task<PaginatedResult<EventDto>> GetFilteredAsync(IEntityFilter<IEntity> filter)
        {

            var filterEvents = filter as EventFilterDto;

            _logger.LogInformation("Запрос с фильтрацией и пагинацией для {Entity}", filterEvents.GetType().Name);

            // Получаем IQueryable
            var query = await _repository.GetQueryAsync();

            // Применяем фильтрацию 
            var filteredQuery = filter.Apply(query) as IQueryable<IEvent>;

            if (filteredQuery == null)
            {
                throw new InvalidOperationException("Не удалось преминить фильтр");
            }

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
            var dtos = items.Select(e => _mapper.Map<EventDto>(e)).ToList();

            _logger.LogInformation($"Возвращено {dtos.Count} элементов из {totalCount}");

            return PaginatedResult<EventDto>.Create(
                dtos,
                totalCount,
                filterEvents.Page ?? 1,
                filterEvents.PageSize ?? totalCount
            );
        }


        public async Task<IResultDto<EventDto>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Запрос события с ID: " + id);
            var eventEntity = await _repository.GetByIdAsync(id);

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Получена модель {eventEntity?.Data?.Title}");
                return ResultDto<EventDto>.Ok(_mapper.Map<EventDto>((Event)eventEntity?.Data), eventEntity?.Message ?? "");
            }

            return ResultDto<EventDto>.Fail(  eventEntity?.Reason ?? "");  
        }

        public async Task<IResultDto<EventDto>> AddAsync(EventDto item)
        {

            _logger.LogInformation("Запрос добавления события с ID: " + item.Id);
            var eventEntity = await _repository.AddAsync(_mapper.Map<Event>(item));
         
            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Добавлена модель: {eventEntity?.Data?.Title}");
                return ResultDto<EventDto>.Ok(item, eventEntity?.Message ?? "");
            }

            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        } 

        public async Task<IResultDto<EventDto>> UpdateAsync(EventDto item)
        {

            _logger.LogInformation("Запрос обновления события с ID: " + item.Id);
            var eventEntity = await _repository.UpdateAsync(_mapper.Map<Event>(item));

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Обновлена модель: {eventEntity?.Data?.Title}");
                return ResultDto<EventDto>.Ok(item, eventEntity?.Message ?? "");
            }

            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        }

        public async Task<IResultDto<EventDto>> DeleteAsync(Guid id)
        {

            _logger.LogInformation("Запрос удаления события с ID: " + id);
            var eventEntity = await _repository.DeleteAsync(id);

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Удалена модель c ID: {id}");
                return ResultDto<EventDto>.Ok(eventEntity?.Message ?? "");
            } 
            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        }

        

        public async Task<IResultDto<EventDto>> AddRangeAsync(IEnumerable<EventDto> items)
        {

            _logger.LogInformation("Запрос добавления списка событий в коллекцию"); 
            var eventEntity = await _repository.AddRangeAsync(items.Select(i=> _mapper.Map<Event>(i)).ToList());

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Добавление моделей данных - успешно");
                return ResultDto<EventDto>.Ok(eventEntity?.Message ?? "");
            } 
            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        }


        public async Task<IResultDto<EventDto>> UpdateRangeAsync(IEnumerable<EventDto> items)
        {

            _logger.LogInformation("Запрос обновления списка событий в коллекции");
            var eventEntity = await _repository.UpdateRangeAsync(items.Select(i => _mapper.Map<Event>(i)).ToList());

            if (eventEntity.IsSuccesfuly){
                _logger.LogInformation($"Обновление моделей данных - успешно");
                return ResultDto<EventDto>.Ok(eventEntity?.Message ?? "");
            }
            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        }


        public bool IsExisted(Guid id) => _repository.IsExisted(id);

        public bool IsExistedByTitle(string name) => _repository.IsExistedByTitle(name);

        
    }
}
