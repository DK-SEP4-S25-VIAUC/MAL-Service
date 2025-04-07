using API.Services.SensorDataService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("sensor")]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataService sensorDataService;

    public SensorDataController(ISensorDataService sensorDataService)
    {
        this.sensorDataService = sensorDataService;
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetSamplesAsync([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        return await sensorDataService.getSamples(from, to);
    }
}