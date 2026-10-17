using Sprints.Events.Domain.Entities;


namespace Sprints.Events.Application.Abstractions.Security;


public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}