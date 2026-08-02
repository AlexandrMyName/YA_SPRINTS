using AutoMapper;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

namespace Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;

public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    { 

        CreateMap<EventInfoDto, Event>();
        CreateMap<IEventInfoDto, Event>();
        CreateMap<IEventInfoDto, IEvent>().As<Event>();


        //CreateMap<CreateEventDto, Event>();
        //CreateMap<ICreateEventDto, Event>();
        //CreateMap<ICreateEventDto, IEvent>().As<Event>();


        CreateMap<BookingInfoDto, Booking>(); 
        CreateMap<IBookingInfoDto, Booking>();
        CreateMap<IBookingInfoDto, IBooking>().As<Booking>();
         
    }
}