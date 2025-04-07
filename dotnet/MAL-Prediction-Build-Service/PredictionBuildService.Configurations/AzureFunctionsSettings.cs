namespace PredictionBuildService.Configurations;

/// <summary>
/// Contains properties for Azure Functions settings, defined in the appsettings.json file.
/// This allows for dependency injection and usage across all classes in the project.
/// </summary>
public class AzureFunctionsSettings
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string ResourceGroupName { get; set; } = string.Empty;
    public string FunctionAppName { get; set; } = string.Empty;
}