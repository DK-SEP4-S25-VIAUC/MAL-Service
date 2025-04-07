using PredictionBuildService.core.EventArgs;

namespace PredictionBuildService.core.Interfaces;

public interface IModelEvaluationService : IEventSubscriber
{
    event Func<object, EvaluatedAllLinearRegressionModelsEventArgs, Task> LinearRegModelsEvaluated;
}