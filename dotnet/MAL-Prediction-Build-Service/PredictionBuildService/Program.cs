using Azure.Identity;
using Azure.ResourceManager;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.Infrastructure;

namespace PredictionBuildService;

/// <summary>
///  Initializes the entire prediction service.
/// </summary>
/// <remarks>
/// This class initializes a worker thread as a background service, as well as initializing
/// and setting up dependency injection for used internal services.
/// </remarks>
public class Program
{
    /// <summary>
    ///  Initializes the entire prediction service.
    /// </summary>
    /// /// <remarks>
    /// Shared internal services are declared as Singletons for dependency injection project-wide.
    /// </remarks>
    public static void Main(string[] args)
    {
        // 1. Continuously monitor Azure Blob Storage for Changes.
        // This is done with Azure Event Grid.
        
        // 2. Process the Change Event:
        // When a new or updated ONNX model is detected, extract metadata (e.g., model type, version) from the blob’s name or metadata.
        
        // 3. Build and Deploy Azure Functions:
        // Generate or update an Azure Function project dynamically (e.g., a C# Function App) to use the new ONNX model for predictions.
        // Deploy the Azure Function to Azure using the Azure Resource Manager (ARM) SDK
        
        // 4. Run as a Worker Service
        // Use a C# Worker Service to host the logic, listening for events and performing deployments.
        // Integrate with Azure services using the Azure SDK.
        
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure logging:
        builder.Services.AddLogging(logging => {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
        
        // Register Azure clients as singletons:
        builder.Services.AddSingleton(_ =>
            new QueueClient(
                // Note: Find this link inside Blob Storage Account -> Settings -> Endpoints -> Queue Service:
                new Uri("https://onnx1storage1test.queue.core.windows.net/model-changes-queue"),
                new DefaultAzureCredential()));
        
        builder.Services.AddSingleton(_ =>
            new BlobServiceClient(
                new Uri("https://onnx1storage1test.blob.core.windows.net"),
                new DefaultAzureCredential()));
        
        builder.Services.AddSingleton(_ => new ArmClient(new DefaultAzureCredential()));
        
        // Register internal services as Singletons, for dependency injection in the entire project:
        builder.Services.AddSingleton<IBlobStorageMonitorService, BlobStorageMonitorServiceImpl>();
        builder.Services.AddSingleton<IModelEvaluationService, ModelEvaluationServiceImpl>();
        builder.Services.AddSingleton<IBuildService, BuildServiceImpl>();
        builder.Services.AddSingleton<IDeploymentService, DeploymentServiceImpl>();
        builder.Services.AddSingleton<IModelCache, ModelCache>();
        builder.Services.AddSingleton<AzureFunctionDeploymentFactory>();
        
        // Register the Worker service (Runs in the background):
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.RunAsync();
    }
}