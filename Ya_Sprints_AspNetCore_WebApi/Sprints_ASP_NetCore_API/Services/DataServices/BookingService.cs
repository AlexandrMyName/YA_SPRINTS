using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Repositories;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Dtos;
using AutoMapper;


namespace SprintASP_NetCore_API.Services.DataServices;

public class BookingService : BaseDataService<IBookingInfoDto, IBooking>,  IBookingService
{

    public BookingService( 
       IRepository<IBooking> repository,
       IRepository<IEvent> eventRepository, // зависимость по хранилищу событий 
       ILogger<BookingService> logger,
       IMapper mapper) : base(repository,logger,mapper)
    {  
        _eventRepository = eventRepository; 
    } 
     
    private readonly IRepository<IEvent> _eventRepository; 
    private readonly ILogger<BookingService> _logger;
    private readonly IMapper _mapper;
     
    /// <summary>
    /// Создание бронирования по идентификатору события
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId)
    {
         
        var eventResult = await _eventRepository.GetByIdAsync(eventId);
        if (!eventResult.IsSuccesfuly) return ResultDto<IBookingInfoDto>.Fail("Event not found");
          
        var bookingDto = await AddAsync(new BookingInfoDto()
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
    public async Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(Guid bookingId)
    {
        var bookingDto = await GetByIdAsync(bookingId);
        return bookingDto;
    }

    /// <summary>
    /// Обновить бронирование
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public async Task<IResultDto<IBookingInfoDto>> UpdateBookingAsync(IBookingInfoDto item) => await base.UpdateAsync(item);
    
}
