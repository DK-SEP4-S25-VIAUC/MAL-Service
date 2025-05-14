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
    private readonly List<SampleDTO> _fallbackList = new();
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
            var dto = await _httpClient.GetFromJsonAsync<CreateManualThresholdDTO>("soilhumidity/threshold");

            if (dto != null)
            {
                return dto.lowerbound;
            }

            _logger.LogWarning("Threshold response was null. Using fallback.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request to soil humidity API failed. Using fallback threshold.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing JSON response. Using fallback threshold.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred. Using fallback threshold.");
        }

        return 20.0;
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
            var uriBuilder = new UriBuilder(new Uri(_httpClient.BaseAddress!, "sample"));
            var query = HttpUtility.ParseQueryString(string.Empty);

            if (from.HasValue)
                query["from"] = from.Value.ToString("o");

            if (to.HasValue)
                query["to"] = to.Value.ToString("o");

            uriBuilder.Query = query.ToString();
            var finalUri = uriBuilder.ToString();

            var response = await _httpClient.GetAsync(finalUri);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            var root = doc.RootElement;
            var list = root
                .GetProperty("response")
                .GetProperty("list")
                .EnumerateArray()
                .Select(item => item.GetProperty("SampleDTO").Deserialize<SampleDTO>())
                .Where(dto => dto != null)
                .ToList();

            if (list.Count >= 2)
            {
                return new OkObjectResult(list);
            }

            throw new Exception();
        }
        catch (Exception)
        {
            Console.WriteLine("API failed, using fallback list.");

            if (_fallbackList == null || _fallbackList.Count < 2)
            {
                return new NotFoundObjectResult("Fallback list does not contain enough samples.");
            }

            return new OkObjectResult(_fallbackList);
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
                    timestamp = DateTime.Parse(parts[0]),
                    soil_humidity = double.Parse(parts[1]),
                    air_humidity = double.Parse(parts[2]),
                    air_temperature = double.Parse(parts[3]),
                    light_value = double.Parse(parts[4])
                };

                _fallbackList.Add(sample);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading fallback CSV: {ex.Message}");
        }
    }
}
