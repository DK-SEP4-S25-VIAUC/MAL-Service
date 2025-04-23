using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.core.EventArgs;

public class EvaluatedAllLinearRegressionModelsEventArgs : System.EventArgs
{
    public ModelDTO BestModel { get; set; }

    public EvaluatedAllLinearRegressionModelsEventArgs(ModelDTO bestModel) {
        BestModel = bestModel;
    }
}