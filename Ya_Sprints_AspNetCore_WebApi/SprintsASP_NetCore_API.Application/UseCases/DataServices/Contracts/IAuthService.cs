using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Auth;
using SprintASP_NetCore_API.Application.Internal;


namespace SprintsASP_NetCore_API.Application.UseCases.DataServices.Contracts;


public interface IAuthService
{
    Task<IResultDto<TokenResponseDto>> RegisterAsync(RegisterDto dto);
    Task<IResultDto<TokenResponseDto>> LoginAsync(LoginDto dto);
    Task<IResultDto<TokenResponseDto>> RefreshAsync(string refreshToken);
    Task<IResultDto<TokenResponseDto>> LogoutAsync(string? refreshToken);
}