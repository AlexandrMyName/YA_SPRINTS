using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Services.DataServices;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Services;


namespace SprintASP_NetCore_API.Services;


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