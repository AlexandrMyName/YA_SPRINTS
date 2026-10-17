using Sprints.Users.Application.Dtos.EntitiesDtos;


namespace Sprints.Users.Application.Dtos.EntitiesDtos.Auth;


public class TokenResponseDto : IEntityDto
{

    public Guid Id { get; set; }

    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
