using SprintsASP_NetCore_API.Application.UseCases.DataServices.Contracts; 
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Auth;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Auth;
using SprintASP_NetCore_API.Presentation.Extentions;
using SprintASP_NetCore_API.Filters.ActionFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace SprintASP_NetCore_API.Presentation.Controllers;


[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
[ValidateInputModel]
[AllowAnonymous]
public class AuthController : ControllerBase
{

    private readonly IAuthService _authService;
    private readonly IConfiguration _config;


    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }


    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (!result.IsSuccesfuly) return BadRequest(result.Reason);

        SetCookies(result.Data!);
        return Ok(result.Data);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (!result.IsSuccesfuly) return Unauthorized(result.Reason);

        SetCookies(result.Data!);
        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto? dto)
    {
        var refreshToken = dto?.RefreshToken
            ?? Request.Cookies[TokenCookieExtensions.RefreshCookieName];

        var result = await _authService.RefreshAsync(refreshToken ?? string.Empty);
        if (!result.IsSuccesfuly) return Unauthorized(result.Reason);

        SetCookies(result.Data!);
        return Ok(result.Data);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto? dto)
    {
        var refreshToken = dto?.RefreshToken
            ?? Request.Cookies[TokenCookieExtensions.RefreshCookieName];

        var result = await _authService.LogoutAsync(refreshToken);
        Response.ClearAuthCookies();

        return result.IsSuccesfuly ? NoContent() : BadRequest(result.Reason);
    }

    private void SetCookies(TokenResponseDto tokens)
    {
        var refreshDays = _config.GetValue<int>("Jwt:RefreshTokenDays", 7);
        Response.SetAccessTokenCookie(tokens.AccessToken, tokens.ExpiresIn);
        Response.SetRefreshTokenCookie(tokens.RefreshToken, refreshDays);
    }
}
