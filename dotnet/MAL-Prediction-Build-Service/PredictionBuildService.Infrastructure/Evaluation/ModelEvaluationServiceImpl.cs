using Microsoft.Extensions.Logging;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;
using PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

namespace PredictionBuildService.Infrastructure.Evaluation;

/// <summary>
/// Implementation of the IModelEvaluationService interface that handles evaluating identified .
/// </summary>
public class ModelEvaluationServiceImpl : IModelEvaluationService
{
    // Variables
    private readonly ILogger<ModelEvaluationServiceImpl> _logger;
    private readonly IModelCache _modelCache;
    private readonly IBlobStorageMonitorService _blobStorageMonitorService;
    private readonly EvaluationInvoker _evaluationInvoker = new();
    
    // Events this class subscribes to:
    private Func<object, AddedNewModelsEventArgs, Task>? _newModelsAddedEventHandler;
    
    // Events this class fires:
    public event Func<object, EvaluatedAllLinearRegressionModelsEventArgs, Task>? LinearRegModelsEvaluated;

    
    /// <summary>
    /// Primary constructor. It is recommended to use dependency injection to inject the specified arguments, instead of manual injection.
    /// </summary>
    /// <param name="logger">A logging service, that can handle logging of messages</param>
    /// <param name="modelCache">The local in-memory cache that holds all currently registered prediction models.</param>
    /// <param name="blobStorageMonitorService">The observable BlobStorageMonitorService responsible for firing events when specific changes occur in the blob storage.</param>
    public ModelEvaluationServiceImpl(
        ILogger<ModelEvaluationServiceImpl> logger, 
        IModelCache modelCache,
        IBlobStorageMonitorService blobStorageMonitorService) {
        _logger = logger;
        _modelCache = modelCache;
        _blobStorageMonitorService = blobStorageMonitorService;
        
        _logger.LogInformation("ModelEvaluationServiceImpl started at: {time}", DateTimeOffset.Now);
        
        // Subscribe to relevant events from the monitoring service:
        Subscribe();
    }
    
    
    // Define Event Handler invokers:
    /// <summary>
    /// Fires an 'EvaluatedAllLinearRegressionModelsEventArgs' event when all the identified LinearRegressionModels have been evaluated.
    /// Allows for listeners to react to this and do stuff, such as build the best the model and if needed, redeploy.
    /// </summary>
    private async Task OnLinearRegModelsEvaluated(ModelDTO bestModel) {
        // Fire the event if there are more than null subscribers.
        if (LinearRegModelsEvaluated != null) {
            await LinearRegModelsEvaluated.Invoke(this, new EvaluatedAllLinearRegressionModelsEventArgs(bestModel));
        } else {
            _logger.LogWarning("No subscribers to the LinearRegModelsEvaluated event.");
        }
    }
    
    
    // Implement the IEventSubscriber interface:
    // TODO: Test
    // Is the class properly subscribed to NewModelsAdded, after this has run?
    public void Subscribe() {
        _newModelsAddedEventHandler = async (sender, e) => await HandleEventAsync(sender, e);
        _blobStorageMonitorService.NewModelsAdded += _newModelsAddedEventHandler;
        _logger.LogInformation("ModelEvaluationService subscribed to events from BlobStorageMonitorService");
    }

    // TODO: Test
    // Is the class properly unsubscribed to NewModelsAdded, after this has run?
    public void Unsubscribe() {
        if (_newModelsAddedEventHandler != null) {
            _blobStorageMonitorService.NewModelsAdded -= _newModelsAddedEventHandler;
            _newModelsAddedEventHandler = null;
        }
        _logger.LogInformation("ModelEvaluationService unsubscribed to events from BlobStorageMonitorService");
    }

    // TODO: Test
    // Are LinearRegressionModels properly evaluated when AddedNewModelsEventArgs are fired/received?
    // What if an unknown Event is registered?
    public async Task HandleEventAsync(object? sender, EventArgs e) {
        switch (e) {
            case AddedNewModelsEventArgs eventArgs:
                await HandleEventAsync(sender, eventArgs);
                break;
            default:
                _logger.LogWarning("Received event with unexpected EventArgs type: {EventArgsType}", e.GetType().Name);
                break;
        }
    }

    
    // Re-direct each EventArgs type to a specific overloaded implementation of HandleEventAsync method.
    // This allows for future scalability in the number of events this class can handle!
    private async Task HandleEventAsync(object? sender, AddedNewModelsEventArgs e) {
        // Initially we just pick the 1st model from the modelCache and build/deploy this - as proof-of-concept.
        // This should be expanded to perform proper model evaluation
        // TODO: PROPERLY IMPLEMENT THIS METHOD!

        // --------------- Temporary: Proof-of-Concept (Below): ---------------
        
        var models = _modelCache.ListModelsAsync();
        ModelDTO firstModel = null;
        await foreach (var model in models) {
            firstModel = model;
            break;
        }

        if (firstModel == null) {
            _logger.LogWarning("No models found in the cache.");
            return;
        }
        
        _logger.LogInformation("Evaluation found best model to be: Type={ModelType}, Version={ModelVersion}", firstModel.Type, firstModel.TrainingTimestamp);
        
        // Notify Subscribers:
        _logger.LogInformation("Now notifying subscribers...");
        await OnLinearRegModelsEvaluated(firstModel);
        
        // --------------- Temporary: Proof-of-Concept (Above): ---------------
        
        // This method should instead:
        // 1. Read the models from the cache.
        // 2. Execute a method for each main type of model (i.e.)
        //      EvaluateLinearRegressionModelsAsync(List_of_Linear_models);
        //      EvaluateLogisticRegressionModelsAsync(List_of_logistic_models);
        //      etc.
        // 3. Each method should fire a relevant event that can be picked up by the build service.
    }
}