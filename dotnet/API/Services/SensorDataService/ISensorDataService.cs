using Microsoft.AspNetCore.Mvc;

namespace API.Services.SensorDataService;

public interface ISensorDataService
{
    public Task<int> getSoilHumiLowerThresholdAsync();
    Task<IActionResult> getSamples(DateTime? from, DateTime? to);
}