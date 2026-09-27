
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Application.Internal; 
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;


/// <summary>
/// Абстракция сервиса хранилища (IBookingService) - Сервис-хранилище бронирования
/// </summary>
public interface IBookingService : IDataStorageService<IBookingInfoDto, Booking>
{
    Task<IResultDto<IBookingInfoDto>> CreateBookingAsync(Guid eventId);
    Task<IResultDto<IBookingInfoDto>> GetBookingByIdAsync(Guid bookingId);
    Task<IResultDto<IBookingInfoDto>> UpdateBookingAsync(IBookingInfoDto item);
    Task<IEnumerable<IBookingInfoDto>> GetPendingBookingsAsync(int maxCountRange = 1000);
}