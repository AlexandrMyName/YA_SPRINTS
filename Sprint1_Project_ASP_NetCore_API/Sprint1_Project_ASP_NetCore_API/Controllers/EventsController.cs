using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos;
using Sprint1_Project_ASP_NetCore_API.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;


namespace Sprint1_Project_ASP_NetCore_API.Controllers;

 
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")] 
public class EventsController : ControllerBase
{

    public EventsController(IDataStorageService<EventDto> eventsService, IWebHostEnvironment environment)
    {   
        _eventsService = eventsService;
        _environment = environment;
    }
     
    private readonly IDataStorageService<EventDto> _eventsService;
    private readonly IWebHostEnvironment _environment;

    #region Ручки

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
     
    /// <summary>
    /// Метод возвращает список всех событий
    /// </summary>  
    /// <response code="200">Возвращается JSON-структура ApiResult с деталями ответа
    /// и HTTP статус-кодом 200 Ok в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult<IEnumerable<EventDto>>), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet] 
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var eventDtos = await _eventsService.GetAllAsync(); 
            return Ok(eventDtos); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, _environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }

    /// <summary>
    /// Метод добавляет событие
    /// </summary>  
    /// <response code="200">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 200 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost("{index:guid}")]
    public async Task<IActionResult> Create([FromRoute] Guid index, [FromBody] EventDto dto)
    {
        try
        {
            dto.Id = index;
            if (_eventsService.IsExisted(index) || _eventsService.IsExistedByTitle(dto.Title))
            { 
                return Conflict("Уже существует сущность c таким идентификатором или названием");
            }


            var result = await _eventsService.AddAsync(dto);
            if (!result.IsSuccesfuly) return BadRequest(result.Reason ?? "Не удалось обновить");

            return Ok(result?.Message ?? "");
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

            return Ok(result?.Message ?? "");
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
