using Microsoft.Extensions.Logging;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Build;

public class BuildServiceImpl : IBuildService
{ 
    private readonly ILogger<BuildServiceImpl> _logger;
    private readonly IModelEvaluationService _modelEvaluationService;
    private Func<object, EvaluatedAllLinearRegressionModelsEventArgs, Task>? _EvaluatedLinearRegModelEventHandler;
     
    public event Func<object, BuiltLinearRegressionModelForDeploymentEventArgs, Task>? BuiltLinearRegModelForDeployment;
   
    public BuildServiceImpl(
        ILogger<BuildServiceImpl> logger, 
        IModelEvaluationService modelEvaluationService) {
        _logger = logger;
        _modelEvaluationService = modelEvaluationService;
        
        // Subscribe to relevant events from the evaluation service:
        Subscribe();
    }
    
    
    // Define Event Handler invokers:
    private async Task OnBuiltLinearRegModelForDeployment() {
        // Fire the event if there are more than null subscribers.
        if (BuiltLinearRegModelForDeployment != null) {
            await BuiltLinearRegModelForDeployment.Invoke(this, new BuiltLinearRegressionModelForDeploymentEventArgs());
        } else {
            _logger.LogWarning("No subscribers to the BuiltLinearRegModelForDeployment event.");
        }
    }
    
    
    // Implement the IEventSubscriber interface:
    public void Subscribe() {
        _EvaluatedLinearRegModelEventHandler = async (sender, e) => await HandleEventAsync(sender, e);
        _modelEvaluationService.LinearRegModelsEvaluated += _EvaluatedLinearRegModelEventHandler;
        _logger.LogInformation("BuildService subscribed to events from ModelEvaluationService");
    }

    
    public void Unsubscribe() {
        if (_EvaluatedLinearRegModelEventHandler != null) {
            _modelEvaluationService.LinearRegModelsEvaluated -= _EvaluatedLinearRegModelEventHandler;
            _EvaluatedLinearRegModelEventHandler = null;
        }
        _logger.LogInformation("BuildService unsubscribed to events from ModelEvaluationService");
    }

    
    public async Task HandleEventAsync(object? sender, EventArgs e) {
        switch (e) {
            case EvaluatedAllLinearRegressionModelsEventArgs eventArgs:
                await HandleEventAsync(sender, eventArgs);
                break;
            default:
                _logger.LogWarning("Received event with unexpected EventArgs type: {EventArgsType}", e.GetType().Name);
                break;
        }
    }
    
    
    // Re-direct each EventArgs type to a specific overloaded implementation of HandleEventAsync method.
    // This allows for future scalability in the number of events this class can handle!
    private async Task HandleEventAsync(object? sender, EvaluatedAllLinearRegressionModelsEventArgs e) {
        throw new NotImplementedException();
    }
}