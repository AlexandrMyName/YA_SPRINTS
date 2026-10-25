using Sprints.Users.Application.Dtos.EntitiesDtos.Users;
using Sprints.Users.Domain.Entities;
using AutoMapper;


namespace Sprints.Users.Application.Mapping;


public class MappingDtoProfile : Profile
{

    public MappingDtoProfile()
    {

       
        CreateMap<User, UserInfoDto>();
        CreateMap<User, IUserInfoDto>().As<UserInfoDto>();
    }
}