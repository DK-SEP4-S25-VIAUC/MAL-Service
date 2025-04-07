using API.DataEntities;

namespace API.Services.PredictionService;

public interface IPredictionService
{
    public Task<ForecastDTO> GetPredictionAsync(int lowerThreshold);
}