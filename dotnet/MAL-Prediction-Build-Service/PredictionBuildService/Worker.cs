using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService;

public class Worker : BackgroundService
{
    private readonly IBlobStorageMonitorService _monitorService;
    private readonly ILogger<Worker> _logger;

    public Worker(IBlobStorageMonitorService monitorService, ILogger<Worker> logger) {
        _monitorService = monitorService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the monitoring service that tracks the Azure Blob Storage repo for changes. It is also the monitoring
    /// service that fires appropriate actions in other services depending on the registered changes.
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("Prediction Build Service started at: {time}", DateTimeOffset.Now);
        
        // Check if the Model Cache is empty. If so, first read all models available
        // from Azure Blob Storage into the in-memory cache.
        // This is necessary upon service initialization, to ensure old models are properly loaded:
        // TODO: IMPLEMENT
        
        Console.WriteLine("Listing blobs...");
        
        // List blobs in the "models" container
        var containerName = "models";
        var blobNames = await _monitorService.ListBlobsAsync(containerName, stoppingToken);
        _logger.LogInformation("Found {BlobCount} blobs in container '{ContainerName}':", blobNames.Count, containerName);
        foreach (var blobName in blobNames) {
            _logger.LogInformation(" - {BlobName}", blobName);
        }
        
        
        // Start the Azure Blob Storage monitoring service, to look for all future changes in the model registry:
        await _monitorService.MonitorAsync(stoppingToken);
    }
}
