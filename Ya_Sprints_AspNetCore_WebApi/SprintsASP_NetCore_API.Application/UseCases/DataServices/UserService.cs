using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;  
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Application.Internal; 
using Microsoft.Extensions.Logging;
using AutoMapper;
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices;


public class UserService : BaseDataService<IUserInfoDto, User>, IUserService
{

    private readonly IInterceptLockings _interceptLockings;


    public UserService(
        IRepository<User> repository,
        ILogger<UserService> logger,
        IInterceptLockings interceptLockings,
        IMapper mapper) : base(repository, logger, mapper)
    {
        _interceptLockings = interceptLockings;
    }

    #region Чтение  

    public async Task<IResultDto<IUserInfoDto>> GetByLoginAsync(string login)
    {
        var found = await Repository.FindAsync(u => u.Login == login);
        var user = found.FirstOrDefault();

        if (user == null)
            return ResultDto<IUserInfoDto>.Fail($"Пользователь '{login}' не найден");

        return ResultDto<IUserInfoDto>.Ok(Mapper.Map<IUserInfoDto>(user), "");
    }
    #endregion

    #region Promote / Demote  

    public async Task<IResultDto<IUserInfoDto>> PromoteToAdminAsync(string login)
    {
        var found = await Repository.FindAsync(u => u.Login == login);
        var user = found.FirstOrDefault();

        if (user == null)
            return ResultDto<IUserInfoDto>.Fail($"Пользователь '{login}' не найден");

        if (user.Role == UserRole.Admin)
            return ResultDto<IUserInfoDto>.Ok(Mapper.Map<IUserInfoDto>(user), "Уже Admin");

        user.PromoteToAdmin();
        await Repository.UpdateAsync(user);
        await Repository.SaveChangesAsync();

        return ResultDto<IUserInfoDto>.Ok(
            Mapper.Map<IUserInfoDto>(user), "Пользователь повышен до Admin");
    }

    public async Task<IResultDto<IUserInfoDto>> DemoteToUserAsync(string login)
    {
        var found = await Repository.FindAsync(u => u.Login == login);
        var user = found.FirstOrDefault();

        if (user == null)
            return ResultDto<IUserInfoDto>.Fail($"Пользователь '{login}' не найден");

        if (user.Role == UserRole.User)
            return ResultDto<IUserInfoDto>.Ok(Mapper.Map<IUserInfoDto>(user), "Уже User");

        user.DemoteToUser();
        await Repository.UpdateAsync(user);
        await Repository.SaveChangesAsync();

        return ResultDto<IUserInfoDto>.Ok(
            Mapper.Map<IUserInfoDto>(user), "Пользователь понижен до User");
    }
    #endregion


    #region Update — только Login  

    public new async Task<IResultDto<IUserInfoDto>> UpdateAsync(IUserInfoDto item)
    {
        var result = await Repository.GetByIdAsync(item.Id);
        if (!result.IsSuccesfuly || result.Data == null)
            return ResultDto<IUserInfoDto>.Fail($"Пользователь {item.Id} не найден");

        var user = result.Data;
        user.ChangeLogin(item.Login);   // Role — только через Promote/Demote

        await Repository.UpdateAsync(user);
        await Repository.SaveChangesAsync();

        return ResultDto<IUserInfoDto>.Ok(Mapper.Map<IUserInfoDto>(user), "Успешно обновлён");
    }

    #endregion
}