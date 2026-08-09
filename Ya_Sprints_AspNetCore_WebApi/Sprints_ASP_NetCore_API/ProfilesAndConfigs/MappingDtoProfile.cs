using Sprints_Project_ASP_NetCore_API.Data.Entities;
using AutoMapper;
using SprintASP_NetCore_API.Data.Entities;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Events;


namespace Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;


public class MappingDtoProfile : Profile
{

    public MappingDtoProfile()
    {
        CreateMap<Event, EventInfoDto>();

        CreateMap<Booking, BookingInfoDto>();
        CreateMap<Booking, IBookingInfoDto>().As<BookingInfoDto>();

        CreateMap<CreateEventDto, Event>().ForMember(dest => dest.AvailableSeats, opt => opt.MapFrom(src => src.TotalSeats));
        CreateMap<CreateEventDto, EventInfoDto>();
    }
}