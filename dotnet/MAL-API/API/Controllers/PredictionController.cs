using API.DataEntities;
using API.Services.PredictionService;
using API.Services.SensorDataService;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("soilhumidity")] //Routing as "soulhumidity" because the interface contract requires so, but i refuse to call it "SoilhumidityController", as I would have to create a new controller class for each type of prediction, which is not SOLID.

public class PredictionController : ControllerBase
{
    private readonly IPredictionService predictionService;
    private readonly ISensorDataService sensorDataService;
    private readonly ILogger<PredictionController> _logger;

    public PredictionController(IPredictionService predictionService, ISensorDataService sensorDataService, ILogger<PredictionController> logger)
    {
        this.predictionService = predictionService;
        this.sensorDataService = sensorDataService;
        _logger = logger;
    }

    [HttpGet("forecast")]
    public async Task<IActionResult> GetForecast()
    {
        try
        {
            var forecast = await predictionService.GetPredictionAsync();

            if (forecast == null)
            {
                return NotFound("Not enough recent samples available to make a prediction.");
            }

            return Ok(new { forecastDTO = forecast }); // ✅ wrap it here
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the forecast.");
            return StatusCode(500, "An internal server error occurred.");
        }
    }


}