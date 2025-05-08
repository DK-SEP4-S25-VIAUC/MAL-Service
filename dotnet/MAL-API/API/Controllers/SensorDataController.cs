using API.Services.SensorDataService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;

[ApiController]
[Route("sensor")]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataService sensorDataService;
    private readonly ILogger<SensorDataController> _logger;

    public SensorDataController(ISensorDataService sensorDataService, ILogger<SensorDataController> logger)
    {
        this.sensorDataService = sensorDataService;
        _logger = logger;
    }

    [HttpGet("data")]
    public async Task<IActionResult> GetSamplesAsync([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            return await sensorDataService.getSamples(from, to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving sensor sample data.");
            return StatusCode(500, "An internal server error occurred while fetching sensor data.");
        }
    }

    [HttpGet("soilhumiditytreshold")]

    public async Task<IActionResult?> GetSoilHumThresholdAsync()
    {
        try
        {
            return Ok(await sensorDataService.getSoilHumiLowerThresholdAsync());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while retrieving sensor soil humidity threshold.");
            return StatusCode(500, "An internal server error occurred while fetching soil humidity threshold.");
        }
    }
    
}