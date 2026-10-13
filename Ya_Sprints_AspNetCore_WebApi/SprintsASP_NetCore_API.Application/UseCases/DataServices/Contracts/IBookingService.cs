using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Application.Internal; 
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;


/// <summary>
/// Абстракция сервиса хранилища (IBookingService) - Сервис-хранилище бронирования.
/// </summary>
public interface IBookingService : IDataStorageService<IBookingInfoDto, Booking>
{
    /// <summary>
    /// Получить бронь по Id. Проверяет права: пользователь — только свою, Admin — любую.
    /// </summary>
    Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(Guid bookingId, Guid userId, bool isAdmin);

    /// <summary>
    /// Создать бронь от имени пользователя. Проверки: событие существует, не началось,
    /// лимит активных броней пользователя, есть свободные места.
    /// </summary>
    Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId, Guid userId);

    /// <summary>
    /// Отменить бронь. isAdmin = true разрешает отмену чужой брони.
    /// </summary>
    Task<IResultDto<IBookingInfoDto>> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin);

    /// <summary>
    /// Обновить бронь (используется фоновым воркером).
    /// </summary>
    Task<IResultDto<IBookingInfoDto>> UpdateBookingAsync(IBookingInfoDto item);

    Task<IEnumerable<IBookingInfoDto>> GetPendingBookingsAsync(int maxCountRange = 1000);
}