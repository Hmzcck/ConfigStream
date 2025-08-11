using Microsoft.AspNetCore.Mvc;

namespace ConfigStream.Mvc.Web.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            application = "ConfigStream.Mvc.Web"
        });
    }
}