using AutoMapper;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events;
 
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users; 
using SprintASP_NetCore_API.Domain.Entities;



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

        CreateMap<UserInfoDto, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore());

        CreateMap<IUserInfoDto, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore());

    }
}