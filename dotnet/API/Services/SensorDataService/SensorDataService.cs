using API.DataEntities;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Web;

namespace API.Services.SensorDataService;

public class SensorDataService : ISensorDataService
{
    private readonly HttpClient _httpClient;

    public SensorDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int> getSoilHumiLowerThresholdAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<Dictionary<string, CreateManualThresholdDTO>>(
            "soilhumidity/threshhold"
        );

        return response["CreateManualThresholdDTO"].lowerbound;
    }

    public async Task<IActionResult> getSamples(DateTime? from, DateTime? to)
    {
        var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, "Sample"));
        var query = HttpUtility.ParseQueryString(string.Empty);

        if (from.HasValue)
            query["from"] = from.Value.ToString("o");

        if (to.HasValue)
            query["to"] = to.Value.ToString("o");

        uriBuilder.Query = query.ToString();
        var finalUri = uriBuilder.ToString();

        var response = await _httpClient.GetAsync(finalUri);

        if (!response.IsSuccessStatusCode)
            return new StatusCodeResult((int)response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, List<Dictionary<string, SampleDTO>>>>();

        if (result != null && result.TryGetValue("list", out var list))
            return new OkObjectResult(new { list });

        return new NotFoundObjectResult("No sample data found.");
    }
}