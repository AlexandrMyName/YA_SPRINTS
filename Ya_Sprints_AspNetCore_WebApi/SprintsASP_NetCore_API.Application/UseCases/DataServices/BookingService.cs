using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Application.Filters;
using SprintsASP_NetCore_API.Domain.Exceptions;
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.Extensions.Logging;
using AutoMapper;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices;

public class BookingService : BaseDataService<IBookingInfoDto, Booking>, IBookingService
{
    private const int MaxPendingBookingsPerPage = 1000;
    private const int MaxActiveBookingsPerUser = 10;

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

    #region Чтение

    public async Task<IEnumerable<IBookingInfoDto>> GetPendingBookingsAsync(
        int maxCountRange = MaxPendingBookingsPerPage)
    {
        var pendingBookings = await GetFilteredAsync(new BookingFilterDto
        {
            Status = BookingStatus.Pending,
            Page = 1,
            PageSize = maxCountRange,
        });

        return pendingBookings.Items.ToList();
    }

    public async Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(
        Guid bookingId, Guid userId, bool isAdmin)
    {
        var bookingDto = await GetByIdAsync(bookingId);

        if (!bookingDto.IsSuccesfuly || bookingDto.Data == null)
            throw new KeyNotFoundException($"Бронь {bookingId} не найдена");

        // Проверка прав: пользователь — только свою, Admin — любую
        if (!isAdmin && bookingDto.Data.UserId != userId)
            throw new NoRightsException(
                $"Пользователь {userId} не может просматривать чужую бронь {bookingId}");

        return bookingDto;
    }

    #endregion

    #region Создание

    public async Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId, Guid userId)
    {
        var semaphore = _interceptLockings.GetOrAddByEventId(eventId);
        await semaphore.WaitAsync();

        Event? eventEntity = null;
        BookingInfoDto? createdBooking = null;
        bool seatsReserved = false;

        await using var transaction = await _eventRepository.BeginTransactionAsync();

        try
        {
            // 1. Событие существует
            var eventResult = await _eventRepository.GetByIdAsync(eventId);
            if (!eventResult.IsSuccesfuly)
                throw new KeyNotFoundException($"Событие {eventId} не найдено");

            eventEntity = eventResult.Data;
            if (eventEntity == null)
                throw new NullReferenceException("Data was null");

            // 2. Событие уже началось
            if (eventEntity.StartAt <= DateTime.UtcNow)
                throw new EventAlreadyStartedException(eventEntity.Id, eventEntity.StartAt);

            // 3. Лимит активных броней у пользователя
            var activeCount = await CountActiveBookingsAsync(userId);
            if (activeCount >= MaxActiveBookingsPerUser)
                throw new ActiveBookingsLimitExceededException(userId, MaxActiveBookingsPerUser);

            // 4. Есть места
            seatsReserved = eventEntity.TryReserveSeats();
            if (!seatsReserved)
                throw new NoAvailableSeatsException("No available seats for this event");

            var updateResult = await _eventRepository.UpdateAsync(eventEntity);
            if (!updateResult.IsSuccesfuly)
                throw new Exception("Не удалось обновить количество мест: " + updateResult.Reason);

            // 5. Создаём бронь с UserId
            createdBooking = new BookingInfoDto
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                UserId = userId,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null
            };

            var bookingResult = await AddAsync(createdBooking);
            if (!bookingResult.IsSuccesfuly)
                throw new Exception("Не удалось создать бронь");

            await _eventRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Создана бронь {BookingId} для пользователя {UserId} на событие {EventId}",
                createdBooking.Id, userId, eventId);

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

    /// <summary>
    /// Число активных (Pending) броней пользователя.
    /// </summary>
    private async Task<int> CountActiveBookingsAsync(Guid userId)
    {
        var paged = await Repository.GetPagedAsync(
            predicate: b => b.UserId == userId && b.Status == BookingStatus.Pending,
            page: 1,
            pageSize: MaxActiveBookingsPerUser + 1);

        return paged.TotalCount;
    }

    #endregion

    #region Отмена

    public async Task<IResultDto<IBookingInfoDto>> CancelBookingAsync(
        Guid bookingId, Guid userId, bool isAdmin)
    {
        var semaphore = _interceptLockings.GetOrAddByBookingId(bookingId);
        await semaphore.WaitAsync();

        await using var transaction = await Repository.BeginTransactionAsync();

        try
        {
            var bookingResult = await Repository.GetByIdAsync(bookingId);
            if (!bookingResult.IsSuccesfuly || bookingResult.Data == null)
                throw new KeyNotFoundException($"Бронь {bookingId} не найдена");

            var booking = bookingResult.Data;

            // 1. Проверка прав
            if (!isAdmin && booking.UserId != userId)
                throw new NoRightsException(
                    $"Пользователь {userId} не может отменить чужую бронь {bookingId}");

            // 2. Отменяем (внутри Cancel — защита от повторной отмены)
            booking.Cancel();

            var update = await Repository.UpdateAsync(booking);
            if (!update.IsSuccesfuly)
                throw new Exception("Не удалось отменить бронь: " + update.Reason);

            // 3. Возвращаем место на событие — ВНУТРИ той же транзакции
            var eventResult = await _eventRepository.GetByIdAsync(booking.EventId);
            if (eventResult.IsSuccesfuly && eventResult.Data != null)
            {
                eventResult.Data.ReleaseSeats(1);
                await _eventRepository.UpdateAsync(eventResult.Data);
            }

            await Repository.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Бронь {BookingId} отменена пользователем {UserId} (isAdmin={IsAdmin})",
                bookingId, userId, isAdmin);

            return ResultDto<IBookingInfoDto>.Ok(
                _mapper.Map<IBookingInfoDto>(booking), "Успешно отменена");
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

    #endregion

    #region Обновление

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

    #endregion
}