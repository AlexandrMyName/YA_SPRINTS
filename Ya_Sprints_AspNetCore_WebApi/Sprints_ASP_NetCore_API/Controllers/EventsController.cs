using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events;
using SprintASP_NetCore_API.Presentation.Extentions;
using SprintASP_NetCore_API.Application.Filters;
using SprintASP_NetCore_API.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace SprintASP_NetCore_API.Controllers;


[Authorize]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EventsController : ControllerBase
{

    private readonly IEventService _eventsService;
    private readonly IBookingService _bookingsService;


    public EventsController(IEventService eventsService, IBookingService bookingService)
    {
        _eventsService = eventsService;
        _bookingsService = bookingService;
    }


    #region Ручки

    /// <summary>
    /// Создаёт бронирование для события.
    /// </summary>
    [HttpPost("{id}/book")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> BookEvent([FromRoute] Guid id)
    {

        var userId = User.GetUserId();
        var bookingResult = await _bookingsService.CreateBookingAsync(id, userId);

        var booking = bookingResult.Data;
        if (booking == null)
            return Problem(detail: bookingResult.Reason, statusCode: 500);

        var location = Url.Action(
            action: nameof(BookingsController.GetBooking),
            controller: "Bookings",
            values: new { version = "1", id = booking.Id },
            protocol: Request.Scheme);

        return Accepted(location, booking);
    }

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
    /// Метод добавляет коллекцию событий. 
    /// Только для администраторов.
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
    /// Метод обновляет событие. 
    /// Только для администраторов.
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
    /// Метод удаляет событие. 
    /// Только для администраторов.
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