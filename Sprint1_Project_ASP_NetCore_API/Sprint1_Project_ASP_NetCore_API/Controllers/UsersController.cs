using Sprint1_Project_ASP_NetCore_API.Filters;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;


namespace Sprint1_Project_ASP_NetCore_API.Controllers;

 
[Route("api/[controller]")]
public class UsersController : ControllerBase
{

    public UsersController(IMapper mapper)
    { 

        _mapper = mapper;
    }

    private readonly IMapper _mapper;
     
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