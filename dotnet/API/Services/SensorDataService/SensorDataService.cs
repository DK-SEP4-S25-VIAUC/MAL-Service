using API.DataEntities;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Web;

namespace API.Services.SensorDataService;

public class SensorDataService : ISensorDataService
{
    private readonly HttpClient _httpClient;
    private readonly List<Dictionary<string, SampleDTO>> _fallbackList = new();

    public SensorDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        LoadFallbackData(); // Populate fallback list on startup
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
        try
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

            response.EnsureSuccessStatusCode(); // throws for 404, 500, etc.

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, List<Dictionary<string, SampleDTO>>>>();

            if (result != null && result.TryGetValue("list", out var list))
                return new OkObjectResult(new { list });

            return new NotFoundObjectResult("No sample data found.");
        }
        catch (Exception)
        {
            // Return preloaded fallback data
            return new OkObjectResult(new { list = _fallbackList });
        }
    }

    private void LoadFallbackData()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "testdata.csv");

            if (!File.Exists(path))
                return;

            var lines = File.ReadAllLines(path);

            foreach (var line in lines.Skip(1)) // Skip header
            {
                var parts = line.Split(',');

                if (parts.Length != 6)
                    continue;

                var sample = new SampleDTO
                {
                    Timestamp = DateTime.Parse(parts[0]),
                    Soil_Humidity = double.Parse(parts[1]),
                    Air_Humidity = double.Parse(parts[2]),
                    Air_Temperature = double.Parse(parts[3]),
                    Light_Value = double.Parse(parts[4])
                };

                _fallbackList.Add(new Dictionary<string, SampleDTO> { { "SampleDTO", sample } });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading fallback CSV: {ex.Message}");
        }
    }
}
