using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Services;

namespace SprintASP_NetCore_API.Services;

/// <summary>
/// Абстракция сервиса хранилища (IBookingService) - Сервис-хранилище бронирования
/// </summary>
public interface IBookingService : IDataStorageService<IBookingDto>
{

    /// <summary>
    /// создание брони для указанного события
    /// </summary>
    /// <param name="eventId"></param>
    Task<IResultDto<IBookingDto>> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// получение брони по идентификатору
    /// </summary>
    /// <param name="bookingId"></param>
    Task<IResultDto<IBookingDto>> GetBookingByIdAsync(Guid bookingId); 
}
