using Microsoft.AspNetCore.Mvc;
using Sprint1_Project_ASP_NetCore_API.Dtos;

namespace Sprint1_Project_ASP_NetCore_API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{

    /// <summary>
    /// Метод возвращает список продуктов
    /// </summary>
    /// <param name="index">Параметр индекса, для получения номера здания</param>
    /// <response code="200">Возвращается JSON-структура ApiBaseResult с деталями ответа
    /// и HTTP статус-кодом 200 Ok в случае успеха</response>
    [ProducesResponseType(typeof(ApiResult), StatusCodes.Status200OK)]
    [Produces("application/json")]
    [HttpGet("{index:int}")]
    [HttpGet]
    public ApiResult<string> GetAll([FromRoute] int index)
    { 
        return ApiResult<string>.Ok("Product List");
    }
}