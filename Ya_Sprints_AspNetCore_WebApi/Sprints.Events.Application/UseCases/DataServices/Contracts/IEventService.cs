
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;  
using Sprints.Events.Application.Internal;
using Sprints.Events.Domain.Entities;


namespace Sprints.Events.Application.UseCases.DataServices.Contracts;


public interface IEventService : IDataStorageService<IEventInfoDto, Event>
{
    Task<IResultDto<IEventInfoDto>> CreateEventAsync(ICreateEventDto dto);
    Task<IResultDto<IEventInfoDto>> CreateEventsRangeAsync(IEnumerable<ICreateEventDto> dtos);
    Task<IResultDto<IEventInfoDto>> UpdateEventAsync(IEventInfoDto item);
    Task ReleaseSeatsAndUpdateAsync(Guid eventId, int count);
}
