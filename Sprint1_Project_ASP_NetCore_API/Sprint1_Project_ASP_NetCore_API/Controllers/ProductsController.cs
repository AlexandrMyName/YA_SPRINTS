using Microsoft.AspNetCore.Mvc;

namespace Sprint1_Project_ASP_NetCore_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        var products = new[]
        {
                new { Id = 1, Name = "Laptop" },
                new { Id = 2, Name = "Mouse" }
            };
        return Ok(products);
    }
}