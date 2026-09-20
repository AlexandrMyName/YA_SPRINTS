
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events;  
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;


public interface IEventService : IDataStorageService<IEventInfoDto, Event>
{
    Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto);
    Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item);
    Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count);
}
