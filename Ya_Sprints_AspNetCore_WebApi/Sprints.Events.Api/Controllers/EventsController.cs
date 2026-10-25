using Sprints.Events.Application.UseCases.DataServices.Contracts;
using Sprints.Events.Application.Dtos.EntitiesDtos.Events;
using Sprints.Events.Application.Filters;
using Microsoft.AspNetCore.Authorization;
using Sprints.Events.Application.Dtos;
using Microsoft.AspNetCore.Mvc;


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
    /// Метод возвращает топ-N самых популярных событий (по числу занятых мест).
    /// Кешируется в Redis (ключ events:top10, TTL 60 c).
    /// </summary>
    [ProducesResponseType(typeof(IEnumerable<EventInfoDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet("top")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTop(
        [FromQuery] int take = 10,
        CancellationToken ct = default)
    {
        // Защита от отрицательных/нулевых значений — иначе .Take(-1) вернёт пустое.
        // Верхняя граница (take > 10) обрезается внутри EventsService до cacheSize.
        if (take <= 0) take = 10;

        var top = await _eventsService.GetTopAsync(take, ct);
        return Ok(top);
    }

    /// <summary>
    /// Метод возвращает событие по идентификатору.
    /// Кешируется в Redis (ключ events:event:{id}, TTL 300 c).
    /// </summary>
    [ProducesResponseType(typeof(ApiResult<EventInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpGet("{index:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromRoute] Guid index)
    {
        var eventDto = await _eventsService.GetByIdAsync(index);
        return eventDto.IsSuccesfuly
            ? Ok(eventDto.Data)
            : Problem(detail: eventDto.Reason, statusCode: StatusCodes.Status404NotFound);
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
    [ProducesResponseType(typeof(EventInfoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEventDto createDto)
    {
        var result = await _eventsService.CreateEventAsync(createDto);
        if (!result.IsSuccesfuly || result.Data == null)
            return Problem(detail: result.Reason, statusCode: StatusCodes.Status400BadRequest);

        return CreatedAtAction(
            actionName: nameof(Get),
            routeValues: new { version = "1", index = result.Data.Id },
            value: result.Data);
    }

    /// <summary>
    /// Метод добавляет коллекцию событий. Только для администраторов.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    [HttpPost("range")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreateEventDto> dtos)
    {
        var result = await _eventsService.CreateEventsRangeAsync(dtos);
        if (!result.IsSuccesfuly)
            return Problem(detail: result.Reason, statusCode: StatusCodes.Status400BadRequest);

        return StatusCode(
            StatusCodes.Status201Created,
            new { message = result.Message });
    }

    /// <summary>
    /// Метод обновляет коллекцию событий. Только для администраторов.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRange([FromBody] IEnumerable<EventInfoDto> dtos)
    {
        var result = await _eventsService.UpdateRangeAsync(dtos);
        if (!result.IsSuccesfuly)
            return Problem(detail: result.Reason, statusCode: StatusCodes.Status400BadRequest);

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Метод обновляет событие. Только для администраторов.
    /// </summary>
    [ProducesResponseType(typeof(EventInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpPut("{index:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromRoute] Guid index, [FromBody] EventInfoDto dto)
    {
        dto.Id = index;

        var result = await _eventsService.UpdateAsync(dto);
        if (!result.IsSuccesfuly)
            return Problem(detail: result.Reason, statusCode: StatusCodes.Status404NotFound);

        return Ok(result.Data ?? dto);
    }

    /// <summary>
    /// Метод удаляет событие. Только для администраторов.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    [HttpDelete("{index:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid index)
    {
        var result = await _eventsService.DeleteAsync(index);
        if (!result.IsSuccesfuly)
            return Problem(detail: result.Reason, statusCode: StatusCodes.Status404NotFound);

        return NoContent();
    }

    #endregion
}