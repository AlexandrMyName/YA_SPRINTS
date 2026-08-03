using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Entities;
using AutoMapper;


namespace Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;


public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    { 

        CreateMap<EventInfoDto, Event>();
        CreateMap<IEventInfoDto, Event>();
        CreateMap<IEventInfoDto, IEvent>().As<Event>();
         
        CreateMap<BookingInfoDto, Booking>(); 
        CreateMap<IBookingInfoDto, Booking>();
        CreateMap<IBookingInfoDto, IBooking>().As<Booking>();
         
    }
}