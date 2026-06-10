using Sprint1_Project_ASP_NetCore_API.Data.Dtos.EntitiesDtos;
using Sprint1_Project_ASP_NetCore_API.Data.Dtos;
using Sprint1_Project_ASP_NetCore_API.Services;
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
    public async Task<ApiResult<EventDto>> Get([FromRoute] Guid index)
    {
        try
        {
            var eventDto = await _eventsService.GetByIdAsync(index);
            return eventDto.IsSuccesfuly ? ApiResult<EventDto>.Ok(eventDto?.Data ?? new EventDto()
            { Title = "", EndAt = default, StartAt = default, Id = default}, eventDto?.Message ?? "")
            : ApiResult<EventDto>.NotFound<EventDto>(eventDto.Reason ?? "");
        }
        catch (Exception ex) {
            return ApiResult<EventDto>.ServerError<EventDto>(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR"); 
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
    public async Task<ApiResult<IEnumerable<EventDto>>> GetAll()
    {
        try
        {
            var eventDtos = await _eventsService.GetAllAsync();
            return ApiResult<IEnumerable<EventDto>>.Ok(eventDtos, "Получено элементов: " + eventDtos.Count().ToString()); 
        }
        catch (Exception ex)
        {
            return ApiResult<IEnumerable<EventDto>>
                .ServerError<IEnumerable<EventDto>>(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
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
    public async Task<ApiResult> Create([FromRoute] Guid index, [FromBody] EventDto dto)
    {
        try
        {
            dto.Id = index;
            if (_eventsService.IsExisted(index))
            {
                return ApiResult.Fail("Уже существует указанная сущность");
            }

            var result = await _eventsService.AddAsync(dto);

            return result.IsSuccesfuly ? ApiResult.Created(result?.Message ?? "") : ApiResult.NotFound(result.Reason ?? "");
        }
        catch (Exception ex)
        {
            return ApiResult.ServerError(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }

    /// <summary>
    /// Метод добавляет - коллекцию событий
    /// </summary>  
    /// <response code="201">Возвращается JSON-структура ApiResult
    /// и HTTP статус-кодом 201 Created в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status201Created)]
    [Produces("application/json")]
    [HttpPost]
    public async Task<ApiResult> CreateRange([FromBody] IEnumerable<EventDto> dtos)
    {
        try
        {
            List<string> existedEvents = new(0);

            foreach (var d in dtos) {
                if (_eventsService.IsExisted(d.Id))
                {
                    existedEvents.Add($"{d.Id}:{d.Title}");
                }
            }

            if (existedEvents.Count > 0)
            {
                return ApiResult.Fail("Уже существуют указанные сущности: " + string.Join(", ", existedEvents));
            }

            var result = await _eventsService.AddRangeAsync(dtos);

            return result.IsSuccesfuly ? ApiResult.Created(result?.Message ?? "") : ApiResult.NotFound(result.Reason ?? "");
        } 
        catch (Exception ex)
        {
            return ApiResult.ServerError(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
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
    public async Task<ApiResult> UpdateRange([FromBody] IEnumerable<EventDto> dtos)
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

            if (notExistedEvents.Count > 0)
            {
                return ApiResult.Fail("Не существуют указанные сущности: " + string.Join(", ", notExistedEvents));
            }

            var result = await _eventsService.UpdateRangeAsync(dtos);

            return result.IsSuccesfuly ? ApiResult.Ok(result?.Message ?? "") : ApiResult.NotFound(result.Reason ?? "");
        }
        catch (Exception ex)
        {
            return ApiResult.ServerError(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
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
    public async Task<ApiResult> Update([FromRoute] Guid index, [FromBody] EventDto dto)
    {
        try
        {
            dto.Id = index;
            if (!_eventsService.IsExisted(index))
            {
                return ApiResult.Fail("Не существует указанная сущность");
            }
             
            var result = await _eventsService.UpdateAsync(dto);

            return result.IsSuccesfuly ? ApiResult.Ok(result?.Message ?? "") : ApiResult.NotFound(result.Reason ?? "");
        }
        catch (Exception ex)
        {
            return ApiResult.ServerError(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
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
    public async Task<ApiResult> Delete([FromRoute] Guid index)
    {
        try
        { 
            if (!_eventsService.IsExisted(index))
            {
                return ApiResult.Fail("Не существует указанная сущность");
            }

            var result = await _eventsService.DeleteAsync(index); 
            return result.IsSuccesfuly ? ApiResult.Ok(result?.Message ?? "") : ApiResult.NotFound(result.Reason ?? "");
        }
        catch (Exception ex)
        {
            return ApiResult.ServerError(_environment.IsDevelopment() ? $"{ex.Message} | {ex.InnerException?.Message ?? ""}" : "SERVER ERROR");
        }
    }

    #endregion
}
