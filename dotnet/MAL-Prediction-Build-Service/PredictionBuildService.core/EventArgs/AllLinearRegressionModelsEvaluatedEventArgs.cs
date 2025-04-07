namespace PredictionBuildService.core.EventArgs;

public class AllLinearRegressionModelsEvaluatedEventArgs : System.EventArgs
{
    public ModelDTO BestModel { get; set; }

    public AllLinearRegressionModelsEvaluatedEventArgs(ModelDTO bestModel) {
        BestModel = bestModel;
    }
}