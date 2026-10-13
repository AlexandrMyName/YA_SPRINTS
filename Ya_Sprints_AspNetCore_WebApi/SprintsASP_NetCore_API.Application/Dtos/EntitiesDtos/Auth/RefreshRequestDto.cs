

namespace SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Auth;


public class RefreshRequestDto
{
    /// <summary>
    /// Опционально — если пусто, берём из cookie.
    /// </summary>
    public string? RefreshToken { get; set; }
}
