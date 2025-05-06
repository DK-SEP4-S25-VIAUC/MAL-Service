namespace PredictionBuildService.core.Interfaces;

/// <summary>
/// Interface that defines which publicly accessible methods must be available for all prediction model evaluation workflows.
/// </summary>
public interface IEvaluationWorkflow
{
    /// <summary>
    /// Executes the given evaluation logic associated with the specific evaluation model workflow.
    /// </summary>
    Task ExecuteEvaluationAsync();
}