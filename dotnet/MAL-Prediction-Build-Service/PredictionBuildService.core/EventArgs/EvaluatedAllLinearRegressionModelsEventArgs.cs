using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.core.EventArgs;

/// <summary>
/// A basic EventArg that is used to provide Observer pattern integration in the application.
/// This class is intended to be used to signal that a evaluation of all identified Linear Regression models has completed,
/// and the best one (most optimal one) has been identified as is ready for building and then deployment.
/// </summary>
public class EvaluatedAllLinearRegressionModelsEventArgs : System.EventArgs
{
    public ModelDTO BestModel { get; set; }

    public EvaluatedAllLinearRegressionModelsEventArgs(ModelDTO bestModel) {
        BestModel = bestModel;
    }
}