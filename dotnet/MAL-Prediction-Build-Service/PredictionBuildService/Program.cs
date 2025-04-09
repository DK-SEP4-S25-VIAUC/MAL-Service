using Azure.Identity;
using Azure.ResourceManager;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.Infrastructure;
using PredictionBuildService.Configurations;
using Microsoft.Extensions.Options;
using PredictionBuildService.Infrastructure.Build;
using PredictionBuildService.Infrastructure.Evaluation;
using PredictionBuildService.Infrastructure.Monitoring;

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
    public static async Task Main(string[] args)
    {
        // TODO: Update the appsettings.json and appsettings.devevelopment.json files to include the proper Azure data, i.e. replace these values:
        //"ContainerName": "models",
        //"StorageAccountUri": "https://onnx1storage1test.blob.core.windows.net",
        //"QueueUri": "https://onnx1storage1test.queue.core.windows.net/blob-events",
        //"ModelFileType": ".onnx",
        //"ModelMetaDataFormat": ".metadata.json".
        
        var builder = Host.CreateApplicationBuilder(args);
        
        
        // Configure logging:
        builder.Services.AddLogging(logging => {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        
        // Load configuration settings from appsettings.json to respective configuration classes, to enable project wide access to settings through dependency injection:
        builder.Services.Configure<AzureBlobStorageSettings>(builder.Configuration.GetSection("AzureBlobStorage"));
        builder.Services.Configure<AzureFunctionsSettings>(builder.Configuration.GetSection("AzureFunctions"));
        
        
        // Register Azure clients as singletons:
        builder.Services.AddSingleton(provider => {
            var settings = provider.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value;
            return new QueueClient(
                new Uri(settings.QueueUri),
                new DefaultAzureCredential());
        });
        
        builder.Services.AddSingleton(provider => {
            var settings = provider.GetRequiredService<IOptions<AzureBlobStorageSettings>>().Value;
            return new BlobServiceClient(
                new Uri(settings.StorageAccountUri),
                new DefaultAzureCredential());
        });
        
        builder.Services.AddSingleton(_ => new ArmClient(new DefaultAzureCredential()));
        
        
        // Register internal services as Singletons, for dependency injection in the entire project:
        builder.Services.AddSingleton<IModelCache, ModelCache>();
        builder.Services.AddSingleton<IBlobStorageInteractionHelper, BlobStorageInteractionHelperImpl>();
        builder.Services.AddSingleton<IBlobStorageMonitorService, BlobStorageMonitorServiceImpl>();
        builder.Services.AddSingleton<IModelEvaluationService, ModelEvaluationServiceImpl>();
        builder.Services.AddSingleton<IBuildService, BuildServiceImpl>();
        
        // Register the Worker service (Runs in the background):
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        await host.RunAsync();
    }
}