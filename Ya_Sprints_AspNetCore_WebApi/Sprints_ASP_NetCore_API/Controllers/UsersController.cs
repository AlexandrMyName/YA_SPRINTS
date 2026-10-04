using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts; 
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintsASP_NetCore_API.Application.Dtos.Filters;
using SprintASP_NetCore_API.Presentation.Extentions;
using SprintASP_NetCore_API.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; 


namespace SprintASP_NetCore_API.Presentation.Controllers;


[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = AuthPolicies.AdminRs256Only)]
public class UsersController : ControllerBase
{

    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IUserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id)
    {

        var result = await _userService.GetByIdAsync(id);
        return result.IsSuccesfuly ? Ok(result.Data) : NotFound(result.Reason);
    }


    [HttpGet("by-login/{login}")]
    [ProducesResponseType(typeof(IUserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByLogin([FromRoute] string login)
    {
        var result = await _userService.GetByLoginAsync(login);
        return result.IsSuccesfuly ? Ok(result.Data) : NotFound(result.Reason);
    }


    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<IUserInfoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] UserFilterDto filter)
    {
        var result = await _userService.GetFilteredAsync(filter);
        return Ok(result);
    }


    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(IUserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UserInfoDto dto)
    {
        dto.Id = id;
        var result = await _userService.UpdateAsync(dto);
        return result.IsSuccesfuly ? Ok(result.Data) : BadRequest(result.Reason);
    }


    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var result = await _userService.DeleteAsync(id);
        return result.IsSuccesfuly ? Ok(result.Message) : NotFound(result.Reason);
    }


    [HttpPost("{login}/promote")]
    [ProducesResponseType(typeof(IUserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Promote([FromRoute] string login)
    {
        var result = await _userService.PromoteToAdminAsync(login);
        return result.IsSuccesfuly ? Ok(result.Data) : NotFound(result.Reason);
    }


    [HttpPost("{login}/demote")]
    [ProducesResponseType(typeof(IUserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Demote([FromRoute] string login)
    {
        var result = await _userService.DemoteToUserAsync(login);
        return result.IsSuccesfuly ? Ok(result.Data) : NotFound(result.Reason);
    }
}
