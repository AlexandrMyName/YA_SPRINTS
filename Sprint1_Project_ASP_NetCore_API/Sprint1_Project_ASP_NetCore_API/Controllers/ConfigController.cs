using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace Sprint1_Project_ASP_NetCore_API.Controllers;

public class AppSettingsResponse
{
    public string Environment { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public int MaxItems { get; set; }
    public bool EnableCache { get; set; }
}
 
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ConfigController(IConfiguration configuration, IWebHostEnvironment environment, IMapper mapper)
    {
        _configuration = configuration;
        _environment = environment;
        _mapper = mapper;
    }
 

    private readonly IMapper _mapper;
    // Недопустимый формат значения поля "Номер декларации на товары".
    // 04607932712429
    // ЕАЭС N RU Д-CN.РА05.В.08686/25
    // 2025-06-11

    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
         
        var response = new AppSettingsResponse
        {
            Environment = _environment.EnvironmentName,
            AppName = _configuration["AppSettings:AppName"] ?? "Unknown",
            MaxItems = _configuration.GetValue<int>("AppSettings:MaxItems"),
            EnableCache = _configuration.GetValue<bool>("AppSettings:EnableCache")
        };

        return Ok(response);
    }
}
