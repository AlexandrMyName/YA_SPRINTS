using SprintsASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Auth;
using SprintsASP_NetCore_API.Application.Abstractions.Security;
using SprintsASP_NetCore_API.Application.Abstractions.Options;
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintASP_NetCore_API.Application.Internal;
using SprintsASP_NetCore_API.Domain.Exceptions; 
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SprintASP_NetCore_API.Domain.Entities;


namespace SprintASP_NetCore_API.Application.UseCases.DataServices;


public class AuthService : IAuthService
{

    private readonly IRepository<User> _users;
    private readonly IRepository<RefreshToken> _refreshTokens;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtOptions _options;

    public AuthService(
        IRepository<User> users,
        IRepository<RefreshToken> refreshTokens,
        IPasswordHasher hasher,
        IJwtTokenGenerator jwt,
        IOptions<JwtOptions> options,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _hasher = hasher;
        _jwt = jwt;
        _logger = logger;
        _options = options.Value;
    }

    #region  Register  

    public async Task<IResultDto<TokenResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var existing = await _users.FindAsync(u => u.Login == dto.Login);
        if (existing.Count > 0)
            throw new UserAlreadyExistsException(dto.Login);

        var user = User.Create(
            Guid.NewGuid(),
            dto.Login,
            _hasher.Hash(dto.Password),
            UserRole.User);          // всегда User

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Зарегистрирован {Login}", user.Login);
        return await IssueTokensAsync(user);
    }
    #endregion



    #region Login  

    public async Task<IResultDto<TokenResponseDto>> LoginAsync(LoginDto dto)
    {
        var found = await _users.FindAsync(u => u.Login == dto.Login);
        var user = found.FirstOrDefault();

        if (user == null || !_hasher.Verify(dto.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        _logger.LogInformation("Успешный вход: {Login}", user.Login);
        return await IssueTokensAsync(user);
    }
    #endregion


    #region Refresh  

    public async Task<IResultDto<TokenResponseDto>> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidRefreshTokenException();

        var hash = _jwt.HashRefreshToken(refreshToken);
        var found = await _refreshTokens.FindAsync(t => t.TokenHash == hash);
        var stored = found.FirstOrDefault();

        if (stored == null || !stored.IsActive)
            throw new InvalidRefreshTokenException();

        var userResult = await _users.GetByIdAsync(stored.UserId);
        if (!userResult.IsSuccesfuly || userResult.Data == null)
            throw new InvalidRefreshTokenException();

        var response = await IssueTokensAsync(userResult.Data);

        // Ротация: помечаем старый как отозванный
        var newHash = _jwt.HashRefreshToken(response.Data!.RefreshToken);
        var newFound = await _refreshTokens.FindAsync(t => t.TokenHash == newHash);
        stored.Revoke(newFound.FirstOrDefault()?.Id);

        await _refreshTokens.UpdateAsync(stored);
        await _refreshTokens.SaveChangesAsync();

        return response;
    }
    #endregion

    #region Logout  

    public async Task<IResultDto<TokenResponseDto>> LogoutAsync(string? refreshToken)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = _jwt.HashRefreshToken(refreshToken);
            var found = await _refreshTokens.FindAsync(t => t.TokenHash == hash);
            var stored = found.FirstOrDefault();

            if (stored != null && stored.RevokedAt == null)
            {
                stored.Revoke();
                await _refreshTokens.UpdateAsync(stored);
                await _refreshTokens.SaveChangesAsync();
            }
        }

        return ResultDto<TokenResponseDto>.Ok("Успешно вышли");
    }
    #endregion


    #region Helper  

    private async Task<IResultDto<TokenResponseDto>> IssueTokensAsync(User user)
    {
        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();

        var stored = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            _jwt.HashRefreshToken(refresh),
            TimeSpan.FromDays(_options.RefreshTokenDays));

        await _refreshTokens.AddAsync(stored);
        await _refreshTokens.SaveChangesAsync();

        return ResultDto<TokenResponseDto>.Ok(new TokenResponseDto
        {
            Id = user.Id,
            AccessToken = access.Token,
            RefreshToken = refresh,
            ExpiresAtUtc = access.ExpiresAtUtc,
            ExpiresIn = (int)(access.ExpiresAtUtc - DateTime.UtcNow).TotalSeconds
        }, "Успешно");
    }

    #endregion
}