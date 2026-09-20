using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events;
using SprintASP_NetCore_API.Domain.Entities;
using AutoMapper;
 


namespace SprintASP_NetCore_API.Application.Mapping;


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