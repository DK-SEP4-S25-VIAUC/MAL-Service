using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.core.EventArgs;

/// <summary>
/// A basic EventArg that is used to provide Observer pattern integration in the application.
/// This class is intended to be used to signal that a evaluation of all identified Linear Regression models has completed,
/// and the best one (most optimal one) has been identified as is ready for building and then deployment.
/// </summary>
public class EvaluatedAllSoilHumidityPredictionModelsEventArgs : System.EventArgs
{
    public ModelDTO BestModel { get; set; }

    public EvaluatedAllSoilHumidityPredictionModelsEventArgs(ModelDTO bestModel) {
        BestModel = bestModel;
    }
}