using Sprints.Users.Domain.Entities;


namespace Sprints.Users.Application.Abstractions.Security;


public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}