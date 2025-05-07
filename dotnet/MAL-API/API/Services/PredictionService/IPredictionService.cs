using API.DataEntities;

namespace API.Services.PredictionService;

public interface IPredictionService
{
    public Task<ForecastDTO> GetPredictionAsync();

    public Task<PredictionInput> BuildPredictionInputAsync();
}