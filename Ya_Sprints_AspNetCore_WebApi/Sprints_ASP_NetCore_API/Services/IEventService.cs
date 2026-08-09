using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using SprintASP_NetCore_API.Data.Dtos.Filters;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Services;

namespace SprintASP_NetCore_API.Services
{
    public interface IEventService : IDataStorageService<EventInfoDto>
    {

        /// <summary>
        /// создание брони для указанного события
        /// </summary>
        /// <param name="eventId"></param>
        Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto); 
        Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item);

        Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count);

    }
}
