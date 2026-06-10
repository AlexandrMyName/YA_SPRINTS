using AutoMapper;
using Microsoft.AspNetCore.Mvc;


namespace Sprint1_Project_ASP_NetCore_API.Controllers;

 
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class EventsController : ControllerBase
{

    public EventsController(IMapper mapper, ILogger<EventsController> logger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    private readonly IMapper _mapper;
    private readonly ILogger<EventsController> _logger;



}
