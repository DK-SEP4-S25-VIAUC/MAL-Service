using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

/// <summary>
/// Holds the implementation logic for evaluating the best available model in prediction the future soil humidity metrics.
/// </summary>
public class EvaluateSoilHumidityWorkflow : IEvaluationWorkflow
{

    /// <summary>
    /// Primary constructor. Takes no external arguments. Automatically registers all workflows defined in classes located in the same folder as this class. It takes the filename, strips the 'Workflow' part and uses that as the key.
    /// <br />
    /// With a workflow class called 'EvaluateSoilHumidityWorkflow' the corresponding command to get this workflow is 'EvaluateSoilHumidity'.
    /// </summary>
    public EvaluateSoilHumidityWorkflow() {
        // TODO: Constructor
        // Will probably need a reference to the ModelCache, so I can load all models that handle soilHumidityPrediction
    }
    
    // TODO: Test
    // 1. Does it actually pick the 'best' model?
    // 2. What if it couldn't pick a model? (I.e. maybe there isn't a model available?)
    public async Task ExecuteEvaluationAsync() {
        throw new NotImplementedException();
    }
}