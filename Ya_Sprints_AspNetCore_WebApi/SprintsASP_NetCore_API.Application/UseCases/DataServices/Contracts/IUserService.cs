using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintASP_NetCore_API.Application.Internal;
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;


public interface IUserService : IDataStorageService<IUserInfoDto, User>
{
    Task<IResultDto<IUserInfoDto>> GetByLoginAsync(string login);
    Task<IResultDto<IUserInfoDto>> PromoteToAdminAsync(string login);
    Task<IResultDto<IUserInfoDto>> DemoteToUserAsync(string login);
}