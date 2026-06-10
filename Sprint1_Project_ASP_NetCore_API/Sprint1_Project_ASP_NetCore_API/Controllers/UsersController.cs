using Microsoft.AspNetCore.Mvc;
using Sprint1_Project_ASP_NetCore_API.Filters;

namespace Sprint1_Project_ASP_NetCore_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// ТЕСТ 1
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ResponseCache(CacheProfileName = "Default")] 
    public IActionResult GetAll()
    {
        return Ok(new[] { "User1", "User2", "User3" });
    }

    // Не кешировать ответ
    // Полезно для персональных или часто меняющихся данных
    [HttpGet("{id}")]
    [LogFilter]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetById(int id)
    {
        return Ok($"User with ID: {id}");
    }

    [HttpPost] 
    public IActionResult Create([FromBody] string userName)
    {
        return CreatedAtAction(nameof(GetById), new { id = 1 }, userName);
    }
}