using System.Text.Json;
using API.DataEntities;
using API.Services.SensorDataService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json.Serialization;

namespace API.Services.PredictionService;

public class PredictionService : IPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ISensorDataService _sensorDataService;

    public PredictionService(HttpClient httpClient, IConfiguration configuration, ISensorDataService sensorDataService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _sensorDataService = sensorDataService;
    }


    public async Task<PredictionInput> BuildPredictionInputAsync()
    {
        DateTime now = DateTime.UtcNow;
        DateTime threeHoursAgo = now.AddHours(-3);

        // Get samples from last 3 hours
        var result = await _sensorDataService.getSamples(threeHoursAgo, now) as OkObjectResult;

        var samples = result?.Value as List<SampleDTO>;

        if (samples == null)
        {
            Console.WriteLine("Fallback data could not be cast to List<SampleDTO>.");
            return null;
        }

        if (samples.Count < 2)
        {
            Console.WriteLine("Not enough samples in fallback data.");
            return null;
        }
        
        // Sort samples descending to get the latest first
        var sortedSamples = samples.OrderByDescending(s => s.timestamp).Take(2).ToList();
        
        


        var latest = sortedSamples[0];
        var previous = sortedSamples[1];
        Console.WriteLine("Latest timestamp:" + latest.timestamp);
        Console.WriteLine("Second latest timestamp:" + previous.timestamp);

        try
        {
            return PredictionInput.FromValues(
                soilHumidity: latest.soil_humidity,
                previousSoilHumidity: previous.soil_humidity,
                airHumidity: latest.air_humidity,
                temperature: latest.air_temperature,
                light: latest.light_value,
                timestamp: latest.timestamp,
                threshold: await _sensorDataService.getSoilHumiLowerThresholdAsync()
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }

    public async Task<ForecastDTO> GetPredictionAsync()
    {
        var apiKey = _configuration["PredictionService:SoilHumidityApiKey"];
        var endpoint = $"PredictSoilHumidity?code={apiKey}";

        var input = await BuildPredictionInputAsync();
        
        if (input == null)
        {
            Console.WriteLine("Input null");
            return null;
        }
        
        var body = new
        {
            inputs = new Dictionary<string, double[]>
            {
                {"soil_humidity", new[] {input.SoilHumidity ?? 0.0}},
                {"soil_delta", new[] {input.SoilDelta}},
                {"air_humidity", new[] {input.AirHumidity ?? 0.0}},
                {"temperature", new[] {input.Temperature ?? 0.0}},
                {"light", new[] {input.Light ?? 0.0}},
                {"hour_sin", new[] {input.HourSin}},
                {"hour_cos", new[] {input.HourCos}},
                {"threshold", new[] {input.Threshold ?? 0.0}}
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(endpoint, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Prediction API returned status code {response.StatusCode}");
        }

        var rawContent = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(rawContent);

        if (!json.TryGetValue("minutes_to_dry", out JToken? minutesToken))
        {
            throw new Exception("Could not find 'minutes_to_dry' in response.");
        }

        double minutes = minutesToken.Value<double>();
        return new ForecastDTO(minutes, DateTime.UtcNow.AddHours(2));
    }
}