using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Dtos;
using AutoMapper;


namespace SprintASP_NetCore_API.Services.DataServices;

public class BookingService : IBookingService
{

    public BookingService(
       IServiceScopeFactory scopeFactory,
       IRepository<IBooking> repository,
       IRepository<IEvent> eventRepository,
       ILogger<BookingService> logger,
       IMapper mapper)
    {
        _scopeFactory = scopeFactory;
        _repository = repository;
        _eventRepository = eventRepository;
        _logger = logger;
        _mapper = mapper;
    }

    private readonly IServiceScopeFactory _scopeFactory; // на будущее когда БД появится мб. переместить лучше в репозиторий

    private readonly IRepository<IEvent> _eventRepository;
    private readonly IRepository<IBooking> _repository;
    private readonly ILogger<BookingService> _logger;
    private readonly IMapper _mapper;
     
    /// <summary>
    /// Создание бронирования по идентификатору события
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<IResultDto<IBookingDto>> CreateBookingAsync(Guid eventId)
    {
         
        var bookingDto = await AddAsync(new BookingDto()
        {
            CreatedAt = DateTime.Now,
            ProcessedAt = null,
            EventId = eventId,
            Status = BookingStatus.Pending,
            Id = Guid.NewGuid(),
        });

        return bookingDto;
    }

    /// <summary>
    /// Получение бронирования по ID 
    /// </summary>
    /// <param name="bookingId"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> GetBookingByIdAsync(Guid bookingId)
    {
        var bookingDto = await GetByIdAsync(bookingId);
        return bookingDto;
    }
      

    #region _ Извлечь абстрактный класс. TODO. Данные повторяются в EventService и BookingService. Логика идентичная. Зависимость по ILogger
     
    /// <summary>
    /// Получение всех бронирований
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<IBookingDto>> GetAllAsync()
    {
        _logger.LogInformation("Запрос всех бронирований");
        var datas = await _repository.GetAllAsync();
        _logger.LogInformation($"Количество: {datas.Count()} данных");
        // действие с Entity (на будущее)
        return datas.Select(e => _mapper.Map<BookingDto>(e)).ToList();
    }

    /// <summary>
    /// Получить бронирования используя фильтр
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<PaginatedResult<IBookingDto>> GetFilteredAsync(IEntityFilter<IEntity> filter)
    {

        var filterEntity = filter as BookingFilterDto;

        _logger.LogInformation("Запрос с фильтрацией и пагинацией для {Entity}", filterEntity.GetType().Name);

        // Получаем IQueryable
        var query = await _repository.GetQueryAsync();

        // Применяем фильтрацию 
        var filteredQuery = filter.Apply(query) as IQueryable<IBooking>;

        if (filteredQuery == null)
        {
            throw new InvalidOperationException("Не удалось преминить фильтр");
        }

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
        var dtos = items.Select(e => _mapper.Map<BookingDto>(e)).ToList();

        _logger.LogInformation($"Возвращено {dtos.Count} элементов из {totalCount}");

        return PaginatedResult<IBookingDto>.Create(
            dtos,
            totalCount,
            filterEntity.Page ?? 1,
            filterEntity.PageSize ?? totalCount
        );
    }

    /// <summary>
    /// Получить бронирование по идентификатору
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Запрос бронирования по ID: " + id);
        var entity = await _repository.GetByIdAsync(id);

        if (entity.IsSuccesfuly)
        {
            _logger.LogInformation($"Получена модель {entity?.Data?.Id}");
            // Маппим в конкретный класс BookingDto (он реализует IBookingDto)
            var dto = _mapper.Map<BookingDto>(entity.Data);
            return ResultDto<IBookingDto>.Ok(dto, entity?.Message ?? "");
        }

        return ResultDto<IBookingDto>.Fail(entity?.Reason ?? "");
    }

    /// <summary>
    /// Добавить бронирование
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> AddAsync(IBookingDto item)
    {

        _logger.LogInformation("Запрос добавления бронирования с ID: " + item.Id);
        var entity = await _repository.AddAsync(_mapper.Map<Booking>(item));

        if (entity.IsSuccesfuly)
        {
            _logger.LogInformation($"Добавлена модель c ID: {entity?.Data?.Id}");
            return ResultDto<IBookingDto>.Ok(item, entity?.Message ?? "");
        }

        return ResultDto<IBookingDto>.Fail(entity?.Reason ?? "");
    }

    /// <summary>
    /// Обновить бронирование
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> UpdateAsync(IBookingDto item)
    {

        _logger.LogInformation("Запрос обновления бронирования с ID: " + item.Id);
        var entity = await _repository.UpdateAsync(_mapper.Map<Booking>(item));

        if (entity.IsSuccesfuly)
        {
            _logger.LogInformation($"Обновлена модель c ID: {entity?.Data?.Id}");
            return ResultDto<IBookingDto>.Ok(item, entity?.Message ?? "");
        }

        return ResultDto<IBookingDto>.Fail(entity?.Reason ?? "");
    }

    /// <summary>
    /// Удалить бронирование
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> DeleteAsync(Guid id)
    {

        _logger.LogInformation("Запрос удаления бронирования с ID: " + id);
        var entity = await _repository.DeleteAsync(id);

        if (entity.IsSuccesfuly)
        {
            _logger.LogInformation($"Удалена модель c ID: {id}");
            return ResultDto<IBookingDto>.Ok(entity?.Message ?? "");
        }
        return ResultDto<IBookingDto>.Fail(entity?.Reason ?? "");
    }

    /// <summary>
    /// Добавить список бронирований
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> AddRangeAsync(IEnumerable<IBookingDto> items)
    { 
        _logger.LogInformation("Запрос добавления списка бронирований в коллекцию");
        var entity = await _repository.AddRangeAsync(items.Select(i => _mapper.Map<Booking>(i)).ToList());

        if (entity.IsSuccesfuly)
        {
            _logger.LogInformation($"Добавление моделей данных - успешно");
            return ResultDto<IBookingDto>.Ok(entity?.Message ?? "");
        }
        return ResultDto<IBookingDto>.Fail(entity?.Reason ?? "");
    }

    /// <summary>
    /// Обновить список бронирований
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingDto>> UpdateRangeAsync(IEnumerable<IBookingDto> items)
    {

        _logger.LogInformation("Запрос обновления списка бронирований в коллекции");
        var entity = await _repository.UpdateRangeAsync(items.Select(i => _mapper.Map<Booking>(i)).ToList());

        if (entity.IsSuccesfuly)
        {
            _logger.LogInformation($"Обновление моделей данных - успешно");
            return ResultDto<IBookingDto>.Ok(entity?.Message ?? "");
        }
        return ResultDto<IBookingDto>.Fail(entity?.Reason ?? "");
    }

    /// <summary>
    /// Проверка наличия по ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool IsExisted(Guid id) => _repository.IsExisted(id);

    /// <summary>
    /// Проверка наличия по Имени
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool IsExistedByTitle(string name) { 
        throw new NotSupportedException("Проверка по названию не поддерживается"); 
    }  
     
    #endregion
}
