using AutoMapper; 
 
using Sprints.Users.Application.Dtos.EntitiesDtos.Users; 
using Sprints.Users.Domain.Entities;



namespace Sprints.Users.Application.Mapping;


public class MappingEntityProfile : Profile
{

    public MappingEntityProfile()
    {
         
        CreateMap<UserInfoDto, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore());

        CreateMap<IUserInfoDto, User>()
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.Role, o => o.Ignore());

    }
}