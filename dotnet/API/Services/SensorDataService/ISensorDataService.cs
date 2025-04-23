using Microsoft.AspNetCore.Mvc;

namespace API.Services.SensorDataService;

public interface ISensorDataService
{
    public Task<double?> getSoilHumiLowerThresholdAsync();
    public Task<double?> GetLatestSoilHumidityValueAsync();
    Task<IActionResult> getSamples(DateTime? from, DateTime? to);
}