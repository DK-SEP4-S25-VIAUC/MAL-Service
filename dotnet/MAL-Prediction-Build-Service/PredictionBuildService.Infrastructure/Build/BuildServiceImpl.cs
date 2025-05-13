using Microsoft.Extensions.Logging;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;

namespace PredictionBuildService.Infrastructure.Build;


/// <summary>
/// This class is responsible for building and deploying/updating associated Azure Functions, that handle each prediction type (I.e. SoilHumidityPredictions, etc.).
/// It subscribes and listens for appropriate events, and acts one these, when a new optimal/best model has been identified for each prediction type.
/// </summary>
/// <remarks>
/// Check out these documentations/tutorials for more info on implementation details:<br />
/// https://learn.microsoft.com/en-us/azure/azure-functions/<br />
/// https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal?pivots=programming-language-csharp<br />
/// https://blog.jetbrains.com/dotnet/2020/10/29/build-serverless-apps-with-azure-functions/<br />
/// </remarks>
public class BuildServiceImpl : IBuildService
{ 
    private readonly ILogger<BuildServiceImpl> _logger;
    private readonly IModelEvaluationService _modelEvaluationService;
    private readonly AzureFunctionsSettings _settings;
    private readonly ArmClient _armClient;
    private Func<object, EvaluatedAllSoilHumidityPredictionModelsEventArgs, Task>? _evaluatedAllSoilHumidityModelsEventHandler;
     
    /// <summary>
    /// Primary constructor. It is recommended to use dependency injection to inject the specified arguments, instead of manual injection.
    /// </summary>
    /// <param name="logger">A logging service, that can handle logging of messages</param>
    /// <param name="modelEvaluationService"></param>
    /// <param name="settings">The settings class, defining access settings to the Azure Functions App containing the functions</param>
    /// <param name="armClient">The Azure Resource Manager Client (ArmClient) that handles interactions with the Azure Functions App on Azure, with proper credentials assigned</param>
    public BuildServiceImpl(
        ILogger<BuildServiceImpl> logger, 
        IModelEvaluationService modelEvaluationService,
        IOptions<AzureFunctionsSettings> settings,
        ArmClient armClient) {
        _logger = logger;
        _modelEvaluationService = modelEvaluationService;
        _settings = settings.Value;
        _armClient = armClient;
        
        _logger.LogInformation("BuildServiceImpl started at: {time}", DateTimeOffset.Now);
        
        // Subscribe to relevant events from the evaluation service:
        Subscribe();
    }
    
    
    // Implement the IEventSubscriber interface:
    // TODO: Test
    // Is the class properly subscribed to NewModelsAdded, after this has run?
    public void Subscribe() {
        _evaluatedAllSoilHumidityModelsEventHandler = async (sender, e) => await HandleEventAsync(sender, e);
        _modelEvaluationService.AllSoilHumidityModelsEvaluated += _evaluatedAllSoilHumidityModelsEventHandler;
        _logger.LogInformation("BuildService subscribed to events from ModelEvaluationService");
    }

    
    // TODO: Test
    // Is the class properly unsubscribed to NewModelsAdded, after this has run?
    public void Unsubscribe() {
        if (_evaluatedAllSoilHumidityModelsEventHandler != null) {
            _modelEvaluationService.AllSoilHumidityModelsEvaluated -= _evaluatedAllSoilHumidityModelsEventHandler;
            _evaluatedAllSoilHumidityModelsEventHandler = null;
        }
        _logger.LogInformation("BuildService unsubscribed to events from ModelEvaluationService");
    }

    
    // TODO: Test
    // Are LinearRegressionModels properly evaluated when EvaluatedAllSoilHumidityPredictionModelsEventArgs are fired/received?
    // What if an unknown Event is registered?
    public async Task HandleEventAsync(object? sender, EventArgs e) {
        switch (e) {
            case EvaluatedAllSoilHumidityPredictionModelsEventArgs eventArgs:
                await HandleEventAsync(sender, eventArgs);
                break;
            default:
                _logger.LogWarning("Received event with unexpected EventArgs type: {EventArgsType}", e.GetType().Name);
                break;
        }
    }
    
    
    // Re-direct each EventArgs type to a specific overloaded implementation of HandleEventAsync method.
    // This allows for future scalability in the number of events this class can handle!
    private async Task HandleEventAsync(object? sender, EvaluatedAllSoilHumidityPredictionModelsEventArgs e) {

        // Deploy updated SoilHumidityPrediction model to Azure Functions:
        if (e.GetType() == typeof(EvaluatedAllSoilHumidityPredictionModelsEventArgs)) {
            try {
                _logger.LogInformation("BuildService received EvaluatedAllSoilHumidityPredictionModelsEventArgs event. Building and deploying updated Linear Model to Azure Functions App");
            
                // Get the Function App resource:
                var resourceGroup = _armClient.GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(_settings.SubscriptionId, _settings.ResourceGroupName));
            
                var functionApp = await resourceGroup.GetWebSiteAsync(_settings.FunctionAppName);

                // Get the current Application Settings:
                var appSettingsResponse = await functionApp.Value.GetApplicationSettingsAsync();
            
                // Get the application settings dictionary:
                var appSettings = appSettingsResponse.Value;

                // Update the OnnxModelUri setting:
                appSettings.Properties[_settings.EnvironmentVariableName_OnnxBestSoilPredictionModelUri] = e.BestModel.DownloadUrl;

                // Apply the updated settings:
                await functionApp.Value.UpdateApplicationSettingsAsync(appSettings);
            
                _logger.LogInformation("Deployed updated azure function reference.\nUpdated {envVar} to {BestModelUrl}", _settings.EnvironmentVariableName_OnnxBestSoilPredictionModelUri, e.BestModel.DownloadUrl);
            
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to build and deploy updated model to Azure Functions.");
                throw;
            }
        }
    }
    
    // HandleEventAsync methods to handle deployment of other prediction types to Azure functions:
    // TODO: If expanding with other prediction types (i.e. PredictAirHumidityLevel), please add methods
    // here targeting the events fired by associated evaluation workflow.
}