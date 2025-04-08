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
/// https://learn.microsoft.com/en-us/azure/azure-functions/
/// https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal?pivots=programming-language-csharp
/// https://blog.jetbrains.com/dotnet/2020/10/29/build-serverless-apps-with-azure-functions/
/// </summary>
public class BuildServiceImpl : IBuildService
{ 
    private readonly ILogger<BuildServiceImpl> _logger;
    private readonly IModelEvaluationService _modelEvaluationService;
    private readonly AzureFunctionsSettings _settings;
    private readonly ArmClient _armClient;
    private Func<object, EvaluatedAllLinearRegressionModelsEventArgs, Task>? _evaluatedLinearRegModelEventHandler;
     
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
    public void Subscribe() {
        _evaluatedLinearRegModelEventHandler = async (sender, e) => await HandleEventAsync(sender, e);
        _modelEvaluationService.LinearRegModelsEvaluated += _evaluatedLinearRegModelEventHandler;
        _logger.LogInformation("BuildService subscribed to events from ModelEvaluationService");
    }

    
    public void Unsubscribe() {
        if (_evaluatedLinearRegModelEventHandler != null) {
            _modelEvaluationService.LinearRegModelsEvaluated -= _evaluatedLinearRegModelEventHandler;
            _evaluatedLinearRegModelEventHandler = null;
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
        try {
            _logger.LogInformation("BuildService received EvaluatedAllLinearRegressionModelsEventArgs event. Building and deploying updated Linear Model to Azure Functions App");
            
            // Get the Function App resource:
            var resourceGroup = _armClient.GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(_settings.SubscriptionId, _settings.ResourceGroupName));
            
            var functionApp = await resourceGroup.GetWebSiteAsync(_settings.FunctionAppName);

            // Get the current Application Settings:
            var appSettingsResponse = await functionApp.Value.GetApplicationSettingsAsync();
            
            // Get the application settings dictionary:
            var appSettings = appSettingsResponse.Value;

            // Update the OnnxModelUri setting:
            appSettings.Properties["OnnxModelUri"] = e.BestModel.DownloadUrl;

            // Apply the updated settings:
            await functionApp.Value.UpdateApplicationSettingsAsync(appSettings);
            
            _logger.LogInformation("Deployed updated azure function reference.\nUpdated OnnxModelUri to {BestModelUrl}", e.BestModel.DownloadUrl);
            
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to build and deploy updated model to Azure Functions.");
            throw;
        }
    }
}