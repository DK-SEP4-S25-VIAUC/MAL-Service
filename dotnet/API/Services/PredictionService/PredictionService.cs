using API.DataEntities;

namespace API.Services.PredictionService;

public class PredictionService : IPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public PredictionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }
    
    
    public async Task<ForecastDTO> GetPredictionAsync(double? lowerThreshold)
    {
        var apiKey = _configuration["PredictionService:SoilHumidityApiKey"];
        var endpoint = $"PredictSoilHumidity?code={apiKey}";

        var body = new
        {
            inputs = new
            {
                target = new[] { lowerThreshold }
            }
        };

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