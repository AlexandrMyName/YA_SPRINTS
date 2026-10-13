using SprintASP_NetCore_API.Domain.Entities;


namespace SprintsASP_NetCore_API.Application.Abstractions.Security;


public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}