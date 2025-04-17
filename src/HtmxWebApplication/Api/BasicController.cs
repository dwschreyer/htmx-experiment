using Microsoft.AspNetCore.Mvc;

namespace HtmxWebApplication.Api;

[Route("api/[controller]")]
[ApiController]
public class BasicController : ControllerBase
{
    [HttpGet("Version")]
    public IActionResult GetVersion()
    {
        return Ok($"1.0.0 @ {DateTime.Now}");
    }
}
