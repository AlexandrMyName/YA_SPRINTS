using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprints.Events.Application.Dtos;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.Filters;
using Sprints.Events.Application.UseCases.DataServices.Contracts;

namespace Sprints.Events.Controllers;


[Authorize]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventsService;

    public EventsController(IEventService eventsService)
    {
        _eventsService = eventsService;
    }

    #region Ручки

    /// <summary>
    /// Метод возвращает событие по идентификатору.
    /// </summary>
    [ProducesResponseType(typeof(ApiResult<EventInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpGet("{index:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromRoute] Guid index)
    {
        var eventDto = await _eventsService.GetByIdAsync(index);
        return eventDto.IsSuccesfuly ? Ok(eventDto.Data) : NotFound(eventDto.Reason);
    }

    /// <summary>
    /// Метод возвращает список событий с пагинацией и фильтрацией.
    /// </summary>
    [ProducesResponseType(typeof(PaginatedResult<EventInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] EventFilterDto? filter = null)
    {
        filter ??= new EventFilterDto { Page = 1, PageSize = 10 };

        var paginatedResult = await _eventsService.GetFilteredAsync(filter);
        return Ok(paginatedResult);
    }

    /// <summary>
    /// Метод добавляет событие. Только для администраторов.
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventDto createDto)
    {
        var result = await _eventsService.CreateEventAsync(createDto);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось создать событие");

        return StatusCode(StatusCodes.Status201Created, result.Message ?? "");
    }

    /// <summary>
    /// Метод добавляет коллекцию событий. Только для администраторов.
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    [HttpPost("range")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreateEventDto> dtos)
    {
        var result = await _eventsService.CreateEventsRangeAsync(dtos);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось создать события");

        return StatusCode(StatusCodes.Status201Created, result.Message ?? "");
    }

    /// <summary>
    /// Метод обновляет коллекцию событий. Только для администраторов.
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRange([FromBody] IEnumerable<EventInfoDto> dtos)
    {
        var result = await _eventsService.UpdateRangeAsync(dtos);
        if (!result.IsSuccesfuly)
            return NotFound(result.Reason ?? "Не удалось обновить");

        return Ok(result.Message ?? "");
    }

    /// <summary>
    /// Метод обновляет событие. Только для администраторов.
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut("{index:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromRoute] Guid index, [FromBody] EventInfoDto dto)
    {
        dto.Id = index;

        var result = await _eventsService.UpdateAsync(dto);
        if (!result.IsSuccesfuly)
            return NotFound(result.Reason ?? "Не удалось обновить");

        return Ok(result.Message ?? "");
    }

    /// <summary>
    /// Метод удаляет событие. Только для администраторов.
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpDelete("{index:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid index)
    {
        var result = await _eventsService.DeleteAsync(index);
        if (!result.IsSuccesfuly)
            return NotFound(result.Reason ?? "Не удалось удалить");

        return Ok(result.Message ?? "");
    }

    #endregion
}