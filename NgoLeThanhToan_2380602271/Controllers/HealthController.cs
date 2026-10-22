using Microsoft.AspNetCore.Mvc;

namespace NgoLeThanhToan_2380602271.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "alive", time = DateTime.UtcNow });
}