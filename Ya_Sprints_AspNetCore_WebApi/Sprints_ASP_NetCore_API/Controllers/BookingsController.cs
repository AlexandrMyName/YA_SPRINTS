using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintASP_NetCore_API.Application.Dtos.EntitiesDtos.Bookings;
using SprintASP_NetCore_API.Presentation.Extentions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace SprintASP_NetCore_API.Controllers;


[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Получает информацию о брони по её идентификатору.
    /// Пользователь видит только свои брони, Admin — любые.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IBookingInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetBooking([FromRoute] Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        var result = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin);
        return Ok(result.Data);
    }

    /// <summary>
    /// Отменяет бронь. Пользователь может отменить только свою,
    /// Admin — любую.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(IBookingInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> CancelBooking([FromRoute] Guid id)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        var result = await _bookingService.CancelBookingAsync(id, userId, isAdmin);
        if (!result.IsSuccesfuly) return BadRequest(result.Reason);

        return Ok(result.Data);
    }
}