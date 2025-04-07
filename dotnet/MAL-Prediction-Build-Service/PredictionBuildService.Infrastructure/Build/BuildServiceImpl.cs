using Microsoft.Extensions.Logging;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.Resources;
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
        AzureFunctionsSettings settings,
        ArmClient armClient) {
        _logger = logger;
        _modelEvaluationService = modelEvaluationService;
        _settings = settings;
        _armClient = armClient;
        
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
            
            Console.WriteLine($"Updated OnnxModelUri to {e.BestModel.DownloadUrl}");
            
        } catch (Exception ex) {
            Console.WriteLine($"Error updating Function App settings: {ex.Message}");
            throw;
        }
    }
}