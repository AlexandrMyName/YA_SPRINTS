using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings; 
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Services.Intercepts;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using AutoMapper;


namespace SprintASP_NetCore_API.Services.DataServices;

public class BookingService : BaseDataService<IBookingInfoDto, IBooking>, IBookingService
{

    private const int MaxPendingBookingsPerPage = 1000;

    public BookingService(
       IRepository<IBooking> repository,
       IRepository<IEvent> eventRepository, // зависимость по хранилищу событий 
       ILogger<BookingService> logger,
       IInterceptLockings interceptLockings,
       IMapper mapper) : base(repository, logger, mapper)
    {
        _interceptLockings = interceptLockings;
        _eventRepository = eventRepository;
    }

    private readonly IRepository<IEvent> _eventRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly IMapper _mapper;
    private readonly IInterceptLockings _interceptLockings;

     

    /// <summary>
    ///  Получает список бронирований в статусе (Pending) 
    /// </summary>
    /// <param name="maxCountRange">Максимальное количество</param>
    /// <returns></returns>
    public async Task<IEnumerable<IBookingInfoDto>> GetPendingBookingsAsync(int maxCountRange = MaxPendingBookingsPerPage)
    {

        var pendingBookings = await GetFilteredAsync(new BookingFilterDto()
        {
            Status = Data.Entities.BookingStatus.Pending,
            Page = 1,
            PageSize = maxCountRange, // Количество бронирований
        });

        return pendingBookings.Items.ToList();

    }
    /// <summary>
    /// Создание бронирования по идентификатору события
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId)
    {

        var semaphore = _interceptLockings.GetOrAddByEventId(eventId);
        await semaphore.WaitAsync();
         
        IEvent? eventEntity = null;
        BookingInfoDto? createdBooking = null;
        bool seatsReserved = false;

        try
        {
            var eventResult = await _eventRepository.GetByIdAsync(eventId);
            if (!eventResult.IsSuccesfuly) throw new KeyNotFoundException($"Событие {eventId} не найдено");
            eventEntity = eventResult.Data;
            if (eventEntity == null) throw new NullReferenceException("Data was null");
             
            seatsReserved = eventEntity.TryReserveSeats();
            if (!seatsReserved) throw new NoAvailableSeatsException("No available seats for this event");

            var updateResult = await _eventRepository.UpdateAsync(eventEntity);
            if (!updateResult.IsSuccesfuly) throw new Exception("Не удалось обновить количество мест");

            createdBooking = new BookingInfoDto()
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };
            var bookingResult = await AddAsync(createdBooking);
            if (!bookingResult.IsSuccesfuly) throw new Exception("Не удалось создать бронь");

            return bookingResult;
        }
        catch
        {
            // Откатываем резервирование мест, если оно было выполнено
            if (seatsReserved && eventEntity != null) eventEntity.ReleaseSeats(1);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Получение бронирования по ID 
    /// </summary>
    /// <param name="bookingId"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(Guid bookingId)
    {
        var bookingDto = await GetByIdAsync(bookingId);

        if (!bookingDto.IsSuccesfuly) throw new KeyNotFoundException("Booking not found");
        return bookingDto;
    }

    /// <summary>
    /// Обновить бронирование
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingInfoDto>> UpdateBookingAsync(IBookingInfoDto item)
    {

        var semaphore = _interceptLockings.GetOrAddByBookingId(item.Id);
        await semaphore.WaitAsync();
        try
        {
           return await base.UpdateAsync(item); 
        }
        finally
        {
            semaphore.Release();
        }  
    }

}
