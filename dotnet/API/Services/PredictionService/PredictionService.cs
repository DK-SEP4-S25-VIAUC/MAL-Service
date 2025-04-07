using API.DataEntities;

namespace API.Services.PredictionService;

public class PredictionService : IPredictionService
{

    public Task<ForecastDTO> GetPredictionAsync(int lowerThreshold)
    {
        throw new NotImplementedException();
        //TODO: Implement calling the prediction MAL service for a prediction.
    }
}