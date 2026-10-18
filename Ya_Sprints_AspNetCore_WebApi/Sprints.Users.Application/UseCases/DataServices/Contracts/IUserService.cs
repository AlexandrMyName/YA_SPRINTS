using Sprints.Users.Application.Dtos.EntitiesDtos.Users;
using Sprints.Users.Application.Internal;
using Sprints.Users.Domain.Entities;


namespace Sprints.Users.Application.UseCases.DataServices.Contracts;


public interface IUserService : IDataStorageService<IUserInfoDto, User>
{
    Task<IResultDto<IUserInfoDto>> GetByLoginAsync(string login);
    Task<IResultDto<IUserInfoDto>> PromoteToAdminAsync(string login);
    Task<IResultDto<IUserInfoDto>> DemoteToUserAsync(string login);
}