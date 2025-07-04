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
    private readonly EvaluationInvoker _evaluationInvoker = new ();
    
    // Events this class subscribes to:
    private Func<object, AddedNewModelsEventArgs, Task>? _newModelsAddedEventHandler;
    
    // Events this class fires:
    public event Func<object, EvaluatedAllSoilHumidityPredictionModelsEventArgs, Task>? AllSoilHumidityModelsEvaluated;

    
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
    /// Fires an 'EvaluatedAllSoilHumidityPredictionModelsEventArgs' event when all the identified LinearRegressionModels have been evaluated.
    /// Allows for listeners to react to this and do stuff, such as build the best the model and if needed, redeploy.
    /// </summary>
    private async Task OnAllSoilPredictionModelsEvaluatedAsync(ModelDTO bestModel) {
        // Fire the event if there are more than null subscribers.
        if (AllSoilHumidityModelsEvaluated != null) {
            await AllSoilHumidityModelsEvaluated.Invoke(this, new EvaluatedAllSoilHumidityPredictionModelsEventArgs(bestModel));
        } else {
            _logger.LogWarning("No subscribers to the AllSoilHumidityModelsEvaluated event.");
        }
    }
    
    
    // Implement the IEventSubscriber interface:
    public void Subscribe() {
        _newModelsAddedEventHandler = async (sender, e) => await HandleEventAsync(sender, e);
        _blobStorageMonitorService.NewModelsAdded += _newModelsAddedEventHandler;
        _logger.LogInformation("ModelEvaluationService subscribed to events from BlobStorageMonitorService");
    }

    public void Unsubscribe() {
        if (_newModelsAddedEventHandler != null) {
            _blobStorageMonitorService.NewModelsAdded -= _newModelsAddedEventHandler;
            _newModelsAddedEventHandler = null;
        }
        _logger.LogInformation("ModelEvaluationService unsubscribed to events from BlobStorageMonitorService");
    }


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
        
        // Read Models from Cache:
        var models = _modelCache.ListModelsAsync();
        ModelDTO? firstModel = null;
        await foreach (var model in models) {
            firstModel = model;
            break;
        }

        // Validate the read models list:
        if (firstModel == null) {
            _logger.LogWarning("No models found in the cache.");
            return;
        }
        
        // Handle all SoilPrediction models:
        try {
            await HandleSoilPredictionEvaluationAsync();
        } catch (Exception ex) {
            _logger.LogError("Exception occured while evaluation for best SoilPredictionModel.\nCause: {}", ex.Message);
            throw new Exception(ex.Message + ex.StackTrace);
        }
        
        // TODO: Add more evaluation methods below for other prediction types,
        // with corresponding private method implementations (i.e. handleAirTemperaturePredictionEvaluation(), etc.)
    }

    
    private async Task HandleSoilPredictionEvaluationAsync() {

        var soilPredictionModels = new List<ModelDTO>();

        await foreach (var model in _modelCache.ListModelsAsync()) {
            if (model.Target != null && model.Target.Contains("minutes_to_dry")) {
                soilPredictionModels.Add(model);
            }
        }
        
        var soilHumidityEvaluationWorkflow = _evaluationInvoker.GetEvaluationWorkflow("EvaluateSoilHumidity");
        var bestSoilPredictionModel = await soilHumidityEvaluationWorkflow.ExecuteEvaluationAsync(soilPredictionModels);
        
        _logger.LogInformation("Evaluation found best SoilHumidityPrediction model to be: Type={ModelType}, Version={ModelVersion}", bestSoilPredictionModel.Type, bestSoilPredictionModel.TrainingTimestamp);
        
        // Notify Subscribers:
        _logger.LogInformation("Now notifying subscribers...");
        await OnAllSoilPredictionModelsEvaluatedAsync(bestSoilPredictionModel);
    }
}