using Sprints_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprints_Project_ASP_NetCore_API.Data.Dtos;
using Sprints_Project_ASP_NetCore_API.Services;
using SprintASP_NetCore_API.Data.Dtos;
using SprintASP_NetCore_API.Services;
using Microsoft.AspNetCore.Mvc;


namespace Sprints_Project_ASP_NetCore_API.Controllers;

 
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")] 
public class EventsController : ControllerBase
{

    public EventsController(IDataStorageService<EventDto> eventsService, IBookingService bookingService, IWebHostEnvironment environment)
    {   
        _eventsService = eventsService;
        _bookingsService = bookingService;
        _environment = environment;
    }
     
    private readonly IDataStorageService<EventDto> _eventsService;
    private readonly IBookingService _bookingsService;
    private readonly IWebHostEnvironment _environment;


    #region Ручки 
     
    /// <summary>
    /// Создаёт бронирование для указанного события.
    /// </summary>
    /// <param name="id">Идентификатор события</param>
    /// <response code="202">Бронирование создано, возвращена информация о нём</response>
    /// <response code="404">Событие с указанным идентификатором не найдено</response>
    [HttpPost("{id}/book")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> BookEvent([FromRoute] Guid id)
    {
   
        var bookingResult = await _bookingsService.CreateBookingAsync(id);
        if (!bookingResult.IsSuccesfuly) return NotFound(bookingResult.Reason);
       
        var booking = bookingResult.Data;
          
        if(booking == null) throw new NullReferenceException(nameof(booking));

        var response = new
        {
            booking.Id,
            EventId = booking.EventId,  
            booking.Status
        };
         
        var location = Url.Action(
            action: "{id}/book",           
            controller: "Events",   
            values: new { bookingId = booking.Id },
            protocol: Request.Scheme
        ) ?? $"/api/bookings/{booking.Id}";

        Response.Headers.Add("Location", location);

        return Accepted(response);
    }




    /// <summary>
    /// Метод возвращает событие по идентификатору
    /// </summary>
    /// <param name="index">Параметр индекса, для получения события</param>
    /// <response code="200">Возвращается JSON-структура с деталями ответа
    /// и HTTP статус-кодом 200 Ok в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult<EventDto>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet("{index:guid}")] 
    public async Task<IActionResult> Get([FromRoute] Guid index)
    {
        try
        {
            var eventDto = await _eventsService.GetByIdAsync(index); 
            return eventDto.IsSuccesfuly ? Ok(eventDto.Data) : NotFound(eventDto.Reason);
        }
        catch (Exception ex) {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR"); 
        }   
    }
     
    /// Метод возвращает список событий с пагинацией и фильтрацией
    /// </summary>  
    /// <response code="200">Возвращает пагинированный список событий</response>
    /// <response code="400">Ошибка валидации фильтра</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [ProducesResponseType(typeof(PaginatedResult<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EventFilterDto? filter = null)
    {
        // Вся валидация и обработка ошибок делегирована ActionFilter и Middleware (GlobalExceptionMiddleware)
        filter ??= new EventFilterDto
        {
            Page = 1,
            PageSize = 10
        };
        // Убрал Try catch. Облегчение endpoint 
        var paginatedResult = await _eventsService.GetFilteredAsync(filter);
        return Ok(paginatedResult);
    }

    /// <summary>
    /// Метод добавляет событие
    /// </summary>  
    /// <response code="200">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 200 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost()]
    public async Task<IActionResult> Create( [FromBody] EventDto dto)
    {
        try
        {

            if (dto.Id == Guid.Empty || dto.Id == default)
            {
                Guid index = Guid.NewGuid();
                dto.Id = index;
            }

            if (_eventsService.IsExisted(dto.Id) || _eventsService.IsExistedByTitle(dto.Title))
            { 
                return Conflict("Уже существует сущность c таким идентификатором или названием");
            }


            var result = await _eventsService.AddAsync(dto);
            if (!result.IsSuccesfuly) return BadRequest(result.Reason ?? "Не удалось обновить");

            return StatusCode(201, result?.Message ?? "");
        }
        catch (Exception ex)
        {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }

    /// <summary>
    /// Метод добавляет - коллекцию событий
    /// </summary>  
    /// <response code="201">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 201 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost("range")]
    public async Task<IActionResult> CreateRange([FromBody] IEnumerable<EventDto> dtos)
    {
        try
        { 
            foreach (var d in dtos) {
                d.Id = Guid.NewGuid();
                while (_eventsService.IsExisted(d.Id)) d.Id = Guid.NewGuid();

                if (_eventsService.IsExistedByTitle(d.Title))
                {
                    return Conflict("Уже существует событие с таким названием: " + d.Title);
                }
            }
             
            var result = await _eventsService.AddRangeAsync(dtos); 
            if (!result.IsSuccesfuly) return BadRequest(result.Reason ?? "Не удалось обновить");
            return StatusCode(201, result?.Message ?? "");
        } 
        catch (Exception ex)
        {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }


    /// <summary>
    /// Метод обновляет список - коллекция событий
    /// </summary>  
    /// <response code="200">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 200 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpPut]
    public async Task<IActionResult> UpdateRange([FromBody] IEnumerable<EventDto> dtos)
    {
        try
        { 
            List<string> notExistedEvents = new(0);

            foreach (var d in dtos)
            {
                if (!_eventsService.IsExisted(d.Id))
                {
                    notExistedEvents.Add($"{d.Id}:{d.Title}");
                }
            }
            
            // В будущем добавить проверку на уникальность названия. Если необходимо его заменить
            if (notExistedEvents.Count > 0)
            {
                return NotFound("Не существуют указанные сущности: " + string.Join(", ", notExistedEvents));
            } 

            var result = await _eventsService.UpdateRangeAsync(dtos);
            if (!result.IsSuccesfuly) return BadRequest(result.Reason ?? "Не удалось обновить");

            return Ok(result?.Message ?? "");
        }
        catch (Exception ex)
        {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }
     
    /// <summary>
    /// Метод обновляет событие
    /// </summary>  
    /// <response code="200">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 200 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpPut("{index:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid index, [FromBody] EventDto dto)
    {
        try
        {
            dto.Id = index;
            if (!_eventsService.IsExisted(index))
            {
                return NotFound("Не существует указанная сущность");
            }

            // В будущем добавить проверку на уникальность названия. Если необходимо его заменить

            var result = await _eventsService.UpdateAsync(dto);
            if (!result.IsSuccesfuly) return BadRequest(result.Reason ?? "Не удалось обновить");

            return Ok(result?.Message ?? "");
        }
        catch (Exception ex)
        {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }
     
    /// <summary>
    /// Метод удаляет событие
    /// </summary>  
    /// <response code="200">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 200 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpDelete("{index:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid index)
    {
        try
        { 
            if (!_eventsService.IsExisted(index)) return NotFound("Не существует указанная сущность");
          
            var result = await _eventsService.DeleteAsync(index); 
            if (!result.IsSuccesfuly) return BadRequest(result.Reason ?? "Не удалось удалить");

            return Ok(result?.Message ?? "");
        }
        catch (Exception ex)
        {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }

    #endregion
}
