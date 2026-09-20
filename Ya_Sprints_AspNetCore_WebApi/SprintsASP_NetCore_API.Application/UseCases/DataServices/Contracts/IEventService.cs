using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events; 
using Sprints_Project_ASP_NetCore_API.Data.Dtos.Internal;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Services;


namespace SprintASP_NetCore_API.Services
{

    public interface IEventService : IDataStorageService<EventInfoDto, Event>
    {
        Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto);
        Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item);
        Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count);
    }
}
