using Sprints.Events.Application.Dtos.EntitiesDtos.Bookings;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;    
using Sprints.Events.Domain.Entities;
using AutoMapper;


namespace Sprints.Events.Application.Mapping;


public class MappingDtoProfile : Profile
{
    public MappingDtoProfile()
    {

        // Event: Entity → DTO  
        CreateMap<Event, EventInfoDto>();
        CreateMap<Event, IEventInfoDto>().As<EventInfoDto>();    

        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src => src.TotalSeats));

        CreateMap<CreateEventDto, EventInfoDto>(); 
    }
}