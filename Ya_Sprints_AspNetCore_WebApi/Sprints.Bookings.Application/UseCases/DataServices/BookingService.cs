using AutoMapper;
using Microsoft.Extensions.Logging;
using Sprints.Bookings.Application.Abstractions;
using Sprints.Bookings.Application.Dtos.EntitiesDtos.Bookings;
using Sprints.Bookings.Application.Filters;
using Sprints.Bookings.Application.Internal;
using Sprints.Bookings.Application.UseCases.DataServices.Contracts;
using Sprints.Bookings.Domain.Entities;
using Sprints.Bookings.Domain.Exceptions;
using Sprints.Bookings.Domain.ValueObjects;
using Sprints.Contracts;

namespace Sprints.Bookings.Application.UseCases.DataServices;

public class BookingService : BaseDataService<IBookingInfoDto, Booking>, IBookingService
{
    private const int MaxPendingBookingsPerPage = 1000;
    private const int MaxActiveBookingsPerUser = 10;

    private readonly ILogger<BookingService> _logger;
    private readonly IMapper _mapper;
    private readonly IInterceptLockings _interceptLockings;
    private readonly IOutboxWriter _outbox;

    public BookingService(
        IRepository<Booking> repository,
        ILogger<BookingService> logger,
        IInterceptLockings interceptLockings,
        IMapper mapper,
        IOutboxWriter outbox) : base(repository, logger, mapper)
    {
        _logger = logger;
        _mapper = mapper;
        _interceptLockings = interceptLockings;
        _outbox = outbox;
    }

    // ===================== Чтение =====================

    public async Task<IEnumerable<IBookingInfoDto>> GetPendingBookingsAsync(
        int maxCountRange = MaxPendingBookingsPerPage)
    {
        var pending = await GetFilteredAsync(new BookingFilterDto
        {
            Status = BookingStatus.Pending,
            Page = 1,
            PageSize = maxCountRange
        });

        return pending.Items.ToList();
    }

    public async Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(
        Guid bookingId, Guid userId, bool isAdmin)
    {
        var dto = await GetByIdAsync(bookingId);

        if (!dto.IsSuccesfuly || dto.Data == null)
            throw new KeyNotFoundException($"Бронь {bookingId} не найдена");

        if (!isAdmin && dto.Data.UserId != userId)
            throw new NoRightsException(
                $"Пользователь {userId} не может просматривать чужую бронь {bookingId}");

        return dto;
    }

    // ===================== Создание =====================

    public async Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId, Guid userId)
    {
        // 1. Лимит активных броней
        var active = await Repository.GetPagedAsync(
            predicate: b => b.UserId == userId && b.Status == BookingStatus.Pending,
            page: 1,
            pageSize: MaxActiveBookingsPerUser + 1);

        if (active.TotalCount >= MaxActiveBookingsPerUser)
            throw new ActiveBookingsLimitExceededException(userId, MaxActiveBookingsPerUser);

        // 2. Создаём Booking (Pending)
        var booking = Booking.Create(
            Guid.NewGuid(),
            eventId,
            userId,
            BookingStatus.Pending,
            DateTime.UtcNow);

        var result = await Repository.AddAsync(booking);
        if (!result.IsSuccesfuly)
            return ResultDto<IBookingInfoDto>.Fail(result.Reason ?? "Не удалось создать бронь");

        // 3. Публикуем BookingRequested в Outbox — В ТОЙ ЖЕ ТРАНЗАКЦИИ.
        // OutboxWriter.AddAsync — только добавляет в контекст, SaveChanges снаружи.
        await _outbox.EnqueueAsync(
            topic: KafkaTopics.BookingRequested,
            key: booking.EventId.ToString(),
            payload: new BookingRequested
            {
                BookingId = booking.Id,
                EventId = booking.EventId,
                UserId = booking.UserId,
                SeatsCount = 1,
                RequestedAt = DateTime.UtcNow
            });

        // 4. Атомарно сохраняем: booking + outbox_message
        await Repository.SaveChangesAsync();

        _logger.LogInformation(
            "Bookings: создана бронь {BookingId} (Pending), BookingRequested отправлен в Outbox. " +
            "User {UserId}, Event {EventId}",
            booking.Id, userId, eventId);

        return ResultDto<IBookingInfoDto>.Ok(
            _mapper.Map<IBookingInfoDto>(booking),
            "Бронь создана, ожидает подтверждения от Events");
    }

 

    public async Task<IResultDto<IBookingInfoDto>> CancelBookingAsync(
        Guid bookingId, Guid userId, bool isAdmin)
    {
        var semaphore = _interceptLockings.GetOrAddByBookingId(bookingId);
        await semaphore.WaitAsync();
        try
        {
            var found = await Repository.GetByIdAsync(bookingId);
            if (!found.IsSuccesfuly || found.Data == null)
                throw new KeyNotFoundException($"Бронь {bookingId} не найдена");

            var booking = found.Data;

            if (!isAdmin && booking.UserId != userId)
                throw new NoRightsException(
                    $"Пользователь {userId} не может отменить чужую бронь {bookingId}");

            booking.Cancel();
            await Repository.UpdateAsync(booking);
            await Repository.SaveChangesAsync();

            return ResultDto<IBookingInfoDto>.Ok(
                _mapper.Map<IBookingInfoDto>(booking), "Успешно отменена");
        }
        finally
        {
            semaphore.Release();
        }
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