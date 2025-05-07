using API.DataEntities;
using API.Services.SensorDataService;
using Microsoft.AspNetCore.Mvc;

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
        var sortedSamples = samples.OrderByDescending(s => s.Timestamp).Take(2).ToList();

        var latest = sortedSamples[0];
        var previous = sortedSamples[1];

        try
        {
            return PredictionInput.FromValues(
                soilHumidity: latest.Soil_Humidity,
                previousSoilHumidity: previous.Soil_Humidity,
                airHumidity: latest.Air_Humidity,
                temperature: latest.Air_Temperature,
                light: latest.Light_Value,
                timestamp: latest.Timestamp,
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
            inputs = new
            {
                soil_humidity = input.SoilHumidity,
                soil_delta = input.SoilDelta,
                air_humidity = input.AirHumidity,
                temperature = input.Temperature,
                light = input.Light,
                hour_sin = input.HourSin,
                hour_cos = input.HourCos,
                threshold = input.Threshold
            }
        };
        
        Console.WriteLine(body);

        var response = await _httpClient.PostAsJsonAsync(endpoint, body);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Prediction API returned status code {response.StatusCode}");
        }

        var rawContent = await response.Content.ReadAsStringAsync();

        var match = System.Text.RegularExpressions.Regex.Match(rawContent, @"\d+");

        if (!match.Success)
        {
            throw new Exception("Could not extract prediction number from response.");
        }

        int minutes = int.Parse(match.Value);
        return new ForecastDTO(minutes);
    }
}