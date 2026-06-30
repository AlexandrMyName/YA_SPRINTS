using AutoMapper;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Entities;

namespace Sprint1_Project_ASP_NetCore_API.ProfilesAndConfigs;

public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    { 
        CreateMap<EventDto, Event>();
    }
}