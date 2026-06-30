 
using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;
using AutoMapper;


namespace Sprint1_Project_ASP_NetCore_API.ProfilesAndConfigs;


public class MappingDtoProfile : Profile
{

    public MappingDtoProfile()
    { 
        CreateMap<Event, EventDto>(); 
    } 
}