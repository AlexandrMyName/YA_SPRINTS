using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;
using Sprint1_Project_ASP_NetCore_API.Repositories;
using AutoMapper;


namespace Sprint1_Project_ASP_NetCore_API.Services.DataServices
{

   

    public class EventsService : IDataStorageService<EventDto>
    {

        public EventsService(IServiceScopeFactory scopeFactory, IRepository<IEvent> eventsRepository, ILogger<EventsService> logger, IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _eventsReposytory = eventsRepository;
            _logger = logger;
            _mapper = mapper;
        }


        private readonly IServiceScopeFactory _scopeFactory; // на будущее когда БД появится мб. переместить лучше в репозиторий
        private readonly IRepository<IEvent> _eventsReposytory;
        private readonly ILogger<EventsService> _logger;
        private readonly IMapper _mapper;

        
        public async Task<IEnumerable<EventDto>> GetAllAsync()
        {
            _logger.LogInformation("Запрос всех событий");
            var events = await _eventsReposytory.GetAllAsync();
            _logger.LogInformation($"Количество: {events.Count()} данных");  
            // действие с Entity (на будущее)
            return events.Select(e=>_mapper.Map<EventDto>(e)).ToList();
        }

        public async Task<IResultDto<EventDto>> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Запрос события с ID: " + id);
            var eventEntity = await _eventsReposytory.GetByIdAsync(id);

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Получена модель {eventEntity?.Data?.Title}");
                return ResultDto<EventDto>.Ok(_mapper.Map<EventDto>(eventEntity?.Data), eventEntity?.Message ?? "");
            }

            return ResultDto<EventDto>.Fail(  eventEntity?.Reason ?? "");  
        }

        public async Task<IResultDto<EventDto>> AddAsync(EventDto item)
        {

            _logger.LogInformation("Запрос добавления события с ID: " + item.Id);
            var eventEntity = await _eventsReposytory.AddAsync(_mapper.Map<Event>(item));
         
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
            var eventEntity = await _eventsReposytory.UpdateAsync(_mapper.Map<Event>(item));

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
            var eventEntity = await _eventsReposytory.DeleteAsync(id);

            if (eventEntity.IsSuccesfuly)
            {
                _logger.LogInformation($"Удалена модель c ID: {id}");
                return ResultDto<EventDto>.Ok(eventEntity?.Message ?? "");
            } 
            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        }

        public bool IsExisted(Guid id) => _eventsReposytory.IsExisted(id);

        public async Task<IResultDto<EventDto>> AddRangeAsync(IEnumerable<EventDto> items)
        {

            _logger.LogInformation("Запрос добавления списка событий в коллекцию"); 
            var eventEntity = await _eventsReposytory.AddRangeAsync(items.Select(i=> _mapper.Map<Event>(i)).ToList());

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
            var eventEntity = await _eventsReposytory.UpdateRangeAsync(items.Select(i => _mapper.Map<Event>(i)).ToList());

            if (eventEntity.IsSuccesfuly){
                _logger.LogInformation($"Обновление моделей данных - успешно");
                return ResultDto<EventDto>.Ok(eventEntity?.Message ?? "");
            }
            return ResultDto<EventDto>.Fail(eventEntity?.Reason ?? "");
        }
    }
}
