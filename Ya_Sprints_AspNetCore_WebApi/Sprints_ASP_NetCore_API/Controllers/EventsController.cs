using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts; 
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Events; 
using SprintASP_NetCore_API.Application.Filters;
using SprintASP_NetCore_API.Application.Dtos;
using SprintASP_NetCore_API.Controllers; 
using Microsoft.AspNetCore.Mvc;
using AutoMapper;


namespace Sprints_Project_ASP_NetCore_API.Controllers;


[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EventsController : ControllerBase
{

    private readonly IEventService _eventsService;
    private readonly IBookingService _bookingsService;
    private readonly IWebHostEnvironment _environment;
    private readonly IMapper _mapper;

    public EventsController(
        IEventService eventsService,
        IBookingService bookingService,
        IWebHostEnvironment environment,
        IMapper mapper)
    {
        _eventsService = eventsService;
        _bookingsService = bookingService;
        _environment = environment;
        _mapper = mapper;
    }

    #region Ручки

    /// <summary>
    /// Создаёт бронирование для указанного события.
    /// </summary>
    [HttpPost("{id}/book")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> BookEvent([FromRoute] Guid id)
    {
        var bookingResult = await _bookingsService.CreateBookingAsync(id);

        var booking = bookingResult.Data;
        if (booking == null) throw new NullReferenceException(nameof(booking));

        var location = Url.Action(
            action: nameof(BookingsController.GetBooking),
            controller: "Bookings",
            values: new { version = "1", id = booking.Id },
            protocol: Request.Scheme);

        return Accepted(location, booking);
    }

    /// <summary>
    /// Метод возвращает событие по идентификатору
    /// </summary>
    [ProducesResponseType(typeof(ApiResult<EventInfoDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet("{index:guid}")]
    public async Task<IActionResult> Get([FromRoute] Guid index)
    {
        var eventDto = await _eventsService.GetByIdAsync(index);
        return eventDto.IsSuccesfuly ? Ok(eventDto.Data) : NotFound(eventDto.Reason);
    }

    /// <summary>
    /// Метод возвращает список событий с пагинацией и фильтрацией
    /// </summary>
    [ProducesResponseType(typeof(PaginatedResult<EventInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EventFilterDto? filter = null)
    {
        filter ??= new EventFilterDto { Page = 1, PageSize = 10 };

        var paginatedResult = await _eventsService.GetFilteredAsync(filter);
        return Ok(paginatedResult);
    }

    /// <summary>
    /// Метод добавляет событие
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventDto createDto)
    {
        if (createDto.Id == Guid.Empty || createDto.Id == default)
            createDto.Id = Guid.NewGuid();

        if (_eventsService.IsExisted(createDto.Id) || _eventsService.IsExistedByTitle(createDto.Title))
            return Conflict("Уже существует сущность c таким идентификатором или названием");

        var result = await _eventsService.CreateEventAsync(createDto);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось обновить");

        return StatusCode(201, result?.Message ?? "");
    }

    /// <summary>
    /// Метод добавляет коллекцию событий
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost("range")]
    public async Task<IActionResult> CreateRange([FromBody] IEnumerable<CreateEventDto> dtos)
    {
        var collectionDto = new List<EventInfoDto>();

        foreach (var d in dtos)
        {
            d.Id = Guid.NewGuid();
            while (_eventsService.IsExisted(d.Id)) d.Id = Guid.NewGuid();

            if (_eventsService.IsExistedByTitle(d.Title))
                return Conflict("Уже существует событие с таким названием: " + d.Title);

            collectionDto.Add(_mapper.Map<EventInfoDto>(d));
        }

        var result = await _eventsService.AddRangeAsync(collectionDto);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось обновить");

        return StatusCode(201, result?.Message ?? "");
    }

    /// <summary>
    /// Метод обновляет коллекцию событий
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpPut]
    public async Task<IActionResult> UpdateRange([FromBody] IEnumerable<EventInfoDto> dtos)
    {
        var notExistedEvents = new List<string>();

        foreach (var d in dtos)
            if (!_eventsService.IsExisted(d.Id))
                notExistedEvents.Add($"{d.Id}:{d.Title}");

        if (notExistedEvents.Count > 0)
            return NotFound("Не существуют указанные сущности: " + string.Join(", ", notExistedEvents));

        var result = await _eventsService.UpdateRangeAsync(dtos);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось обновить");

        return Ok(result?.Message ?? "");
    }

    /// <summary>
    /// Метод обновляет событие
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpPut("{index:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid index, [FromBody] EventInfoDto dto)
    {
        dto.Id = index;
        if (!_eventsService.IsExisted(index))
            return NotFound("Не существует указанная сущность");

        var result = await _eventsService.UpdateAsync(dto);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось обновить");

        return Ok(result?.Message ?? "");
    }

    /// <summary>
    /// Метод удаляет событие
    /// </summary>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpDelete("{index:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid index)
    {
        if (!_eventsService.IsExisted(index))
            return NotFound("Не существует указанная сущность");

        var result = await _eventsService.DeleteAsync(index);
        if (!result.IsSuccesfuly)
            return BadRequest(result.Reason ?? "Не удалось удалить");

        return Ok(result?.Message ?? "");
    }

    #endregion
}