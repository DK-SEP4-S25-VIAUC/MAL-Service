using PredictionBuildService.core.EventArgs;

namespace PredictionBuildService.core.Interfaces;

/// <summary>
/// Interface that defines what the service that evaluates all predictions models must implement. Including required events that implementing classes must fire when done evaluating models.
/// </summary>
public interface IModelEvaluationService : IEventSubscriber
{
    // Events implementing classes must be able to publish/fire:
    event Func<object, EvaluatedAllSoilHumidityPredictionModelsEventArgs, Task> AllSoilHumidityModelsEvaluated;
    
    // Add more Events above if other predictions models are implemented later (i.e. a LogisticRegModelsEvaluated for any logistic regression models, etc.)
}