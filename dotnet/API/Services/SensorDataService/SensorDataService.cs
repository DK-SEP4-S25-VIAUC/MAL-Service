using API.DataEntities;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Web;
using System.Text.Json;


namespace API.Services.SensorDataService;

public class SensorDataService : ISensorDataService
{
    private readonly HttpClient _httpClient;
    private readonly List<Dictionary<string, SampleDTO>> _fallbackList = new();
    private readonly ILogger<SensorDataService> _logger;

    public SensorDataService(HttpClient httpClient, ILogger<SensorDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        LoadFallbackData();
    }

    public async Task<double?> getSoilHumiLowerThresholdAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, CreateManualThresholdDTO>>(
                "soilhumidity/threshhold"
            );

            if (response != null && response.TryGetValue("CreateManualThresholdDTO", out var dto))
            {
                return dto.lowerbound;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to soil humidity API failed.");
        }
        catch (NotSupportedException ex) // Content type is not valid
        {
            _logger.LogError(ex, "The content type of the response is not supported.");
        }
        catch (JsonException ex) // JSON parsing error
        {
            _logger.LogError(ex, "Error parsing JSON response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching soil humidity threshold.");
        }
        
        return null;
    }
    
    public async Task<double?> GetLatestSoilHumidityValueAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, SoilHumidityDTO>>(
                "soilhumidity/latest"
            );

            if (response != null && response.TryGetValue("SoilHumidityDTO", out var dto))
            {
                return dto.Soil_Humidity_Value;
            }

            _logger.LogWarning("SoilHumidityDTO not found in the response.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to fetch latest soil humidity failed.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON for latest soil humidity.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while retrieving latest soil humidity.");
        }

        return null;
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
