using SprintASP_NetCore_API.Services;
using Microsoft.AspNetCore.Mvc;
using SprintASP_NetCore_API.Data.Dtos.EntitiesDtos.Bookings;


namespace SprintASP_NetCore_API.Controllers;


[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BookingsController : ControllerBase
{

    private readonly IBookingService _bookingService;
    private readonly IWebHostEnvironment _environment;

    public BookingsController(IBookingService bookingService, IWebHostEnvironment environment)
    {
        _bookingService = bookingService;
        _environment = environment;
    }

    /// <summary>
    /// Получает информацию о брони по её идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор брони</param>
    /// <response code="200">Возвращает данные брони</response>
    /// <response code="404">Бронь не найдена</response> 
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IBookingInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetBooking([FromRoute] Guid id)
    {

        var result = await _bookingService.GetBookingByIdAsync(id);
        return Ok(result.Data);
    }
}