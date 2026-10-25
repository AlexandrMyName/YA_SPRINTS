using Sprints.Users.Application.Dtos.EntitiesDtos.Users;
using Sprints.Users.Application.Dtos.EntitiesDtos.Auth;
using Sprints.Users.Application.Internal;


namespace Sprints.Users.Application.UseCases.DataServices.Contracts;


public interface IAuthService
{
    Task<IResultDto<TokenResponseDto>> RegisterAsync(RegisterDto dto);
    Task<IResultDto<TokenResponseDto>> LoginAsync(LoginDto dto);
    Task<IResultDto<TokenResponseDto>> RefreshAsync(string refreshToken);
    Task<IResultDto<TokenResponseDto>> LogoutAsync(string? refreshToken);
}