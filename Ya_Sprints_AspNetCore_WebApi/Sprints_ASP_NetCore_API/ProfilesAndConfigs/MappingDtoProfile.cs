 
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Entities;
using AutoMapper;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Data.Entities;


namespace Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;


public class MappingDtoProfile : Profile
{

    public MappingDtoProfile()
    { 
        //events
        CreateMap<Event, EventDto>();

        // Bookings
        CreateMap<IBooking, IBookingDto>();
        CreateMap<Booking, IBookingDto>();
        CreateMap<Booking, BookingDto>();
        CreateMap<IBookingDto, Booking>();
    } 
}