using AutoMapper;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos;
using SprintASP_NetCore_API.Data.Entities;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

namespace Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;

public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    { 

        CreateMap<EventDto, Event>();

        CreateMap<BookingInfoDto, Booking>(); 
        CreateMap<IBookingInfoDto, Booking>();
        CreateMap<IBookingInfoDto, IBooking>().As<Booking>();
         
    }
}