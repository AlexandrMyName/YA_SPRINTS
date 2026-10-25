using AutoMapper;
using Sprints.Events.Application.Dtos.EntitiesDtos.Bookings;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events; 
using Sprints.Events.Domain.Entities;



namespace Sprints.Events.Application.Mapping;


public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    {

        CreateMap<EventInfoDto, Event>();
        CreateMap<IEventInfoDto, Event>();
        CreateMap<IEventInfoDto, IEvent>().As<Event>(); 

    }
}