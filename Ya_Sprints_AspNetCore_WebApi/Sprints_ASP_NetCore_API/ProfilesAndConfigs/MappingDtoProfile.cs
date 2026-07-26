 
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
        CreateMap<Event, EventDto>();
         
        CreateMap<Booking, BookingInfoDto>();
        CreateMap<Booking, IBookingInfoDto>().As<BookingInfoDto>();
    } 
}