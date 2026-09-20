using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Application.UseCases;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings; 
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities; 
using SprintsASP_NetCore_API.Application.Abstractions;


namespace SprintASP_NetCore_API.Services.DataServices;

public class BookingService : BaseDataService<IBookingInfoDto, Booking>, IBookingService
{
    private const int MaxPendingBookingsPerPage = 1000;

    private readonly IRepository<Event> _eventRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly IMapper _mapper;
    private readonly IInterceptLockings _interceptLockings;

    public BookingService(
        IRepository<Booking> repository,
        IRepository<Event> eventRepository,
        ILogger<BookingService> logger,
        IInterceptLockings interceptLockings,
        IMapper mapper) : base(repository, logger, mapper)
    {
        _eventRepository = eventRepository;
        _logger = logger;
        _mapper = mapper;
        _interceptLockings = interceptLockings;
    }

    public async Task<IEnumerable<IBookingInfoDto>> GetPendingBookingsAsync(
        int maxCountRange = MaxPendingBookingsPerPage)
    {
        // BookingFilterDto : IEntityFilter<Booking>  ← компилятор проверит сам
        var pendingBookings = await GetFilteredAsync(new BookingFilterDto
        {
            Status = BookingStatus.Pending,
            Page = 1,
            PageSize = maxCountRange,
        });

        return pendingBookings.Items.ToList();
    }

    public async Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(eventId);
        await semaphore.WaitAsync();

        Event? eventEntity = null;
        BookingInfoDto? createdBooking = null;
        bool seatsReserved = false;

        await using var transaction = await _eventRepository.BeginTransactionAsync();

        try
        {
            var eventResult = await _eventRepository.GetByIdAsync(eventId);
            if (!eventResult.IsSuccesfuly)
                throw new KeyNotFoundException($"Событие {eventId} не найдено");

            eventEntity = eventResult.Data;
            if (eventEntity == null) throw new NullReferenceException("Data was null");

            seatsReserved = eventEntity.TryReserveSeats();
            if (!seatsReserved)
                throw new NoAvailableSeatsException("No available seats for this event");

            var updateResult = await _eventRepository.UpdateAsync(eventEntity);
            if (!updateResult.IsSuccesfuly)
                throw new Exception("Не удалось обновить количество мест: " + updateResult.Reason);

            createdBooking = new BookingInfoDto
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            var bookingResult = await AddAsync(createdBooking);
            if (!bookingResult.IsSuccesfuly)
                throw new Exception("Не удалось создать бронь");

            await _eventRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            return bookingResult;
        }
        catch
        {
            await transaction.RollbackAsync();

            if (seatsReserved && eventEntity != null)
                eventEntity.ReleaseSeats(1);

            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(Guid bookingId)
    {
        var bookingDto = await GetByIdAsync(bookingId);

        if (!bookingDto.IsSuccesfuly)
            throw new KeyNotFoundException("Booking not found");

        return bookingDto;
    }

    public async Task<IResultDto<IBookingInfoDto>> UpdateBookingAsync(IBookingInfoDto item)
    {
        var semaphore = _interceptLockings.GetOrAddByBookingId(item.Id);
        await semaphore.WaitAsync();
        try
        {
            var result = await base.UpdateAsync(item);

            if (result.IsSuccesfuly)
            {
                await Repository.SaveChangesAsync();
                return result;
            }

            throw new Exception("Не удалось обновить сущность: " + result.Reason);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
