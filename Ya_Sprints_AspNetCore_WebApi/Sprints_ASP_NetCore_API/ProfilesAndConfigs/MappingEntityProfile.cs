using AutoMapper;
using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Entities;

namespace Sprints_Project_ASP_NetCore_API.ProfilesAndConfigs;

public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    { 
        CreateMap<EventDto, Event>();
    }
}