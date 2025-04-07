using API.DataEntities;
using API.Services.PredictionService;
using API.Services.SensorDataService;
using Microsoft.AspNetCore.Mvc;

namespace RESTApi.Controllers;

[ApiController]
[Route("soilhumidity")] //Routing as "soulhumidity" because the interface contract requires so, but i refuse to call it "SoilhumidityController", as I would have to create a new controller class for each type of prediction, which is not SOLID.

public class PredictionController : ControllerBase
{
    private readonly IPredictionService predictionService;
    private readonly ISensorDataService sensorDataService;

    public PredictionController(IPredictionService predictionService, ISensorDataService sensorDataService)
    {
        this.predictionService = predictionService;
        this.sensorDataService = sensorDataService;
    }

    [HttpGet("forecast")]
    public async Task<ForecastDTO> GetForecast()
    {
        int lowerThreshold = await sensorDataService.getSoilHumiLowerThresholdAsync();
        var forecast = await predictionService.GetPredictionAsync(lowerThreshold);

        return forecast;
    }
}