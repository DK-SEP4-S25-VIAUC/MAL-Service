using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService;

/// <summary>
/// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-download
/// </summary>
public class Worker : BackgroundService
{
    private readonly IBlobStorageMonitorService _monitorService;
    private readonly ILogger<Worker> _logger;
    private readonly AzureBlobStorageSettings _settings;
    private readonly IModelCache _modelCache;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IBlobStorageInteractionHelper _blobBlobStorageInteractionHelper;
    private readonly IModelEvaluationService _modelEvaluationService;

    public Worker(
        IBlobStorageMonitorService monitorService,
        ILogger<Worker> logger,
        IOptions<AzureBlobStorageSettings> settings,
        IModelCache modelCache,
        BlobServiceClient blobServiceClient,
        IBlobStorageInteractionHelper blobBlobStorageInteractionHelper,
        IModelEvaluationService modelEvaluationService) {
        _monitorService = monitorService;
        _logger = logger;
        _settings = settings.Value;
        _modelCache = modelCache;
        _blobServiceClient = blobServiceClient;
        _blobBlobStorageInteractionHelper = blobBlobStorageInteractionHelper;
        _modelEvaluationService = modelEvaluationService;
    }

    /// <summary>
    /// Executes the monitoring service that tracks the Azure Blob Storage repo for changes. It is also the monitoring
    /// service that fires appropriate actions in other services depending on the registered changes.
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("Prediction Build Service started at: {time}", DateTimeOffset.Now);
        
        // Check if the ModelDTO Cache is empty. If so, first read all models available from Azure Blob Storage into the in-memory cache.
        // This is necessary upon service initialization, to ensure existing models are properly loaded:
        if (_modelCache.CacheSize() == 0) {
            await _blobBlobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(_blobServiceClient, stoppingToken, 
                _settings.ContainerName, 
                _settings.ModelMetaDataFormat, 
                _settings.ModelFileType);
        }

        var models = _modelCache.ListModelsAsync();
        var modelsAsString = "Loaded these models:\n";
        await foreach (var model in models) {
            modelsAsString += "type: " + model.Type + ", version: " + model.Version + "\n";
        }
        _logger.LogInformation("{modelsAsString}", modelsAsString);
        
        // Start the Azure Blob Storage monitoring service, to look for all future changes in the model registry:
        await _monitorService.MonitorAsync(stoppingToken);
        
        // Begin application shutdown (ensure all subscribers are un-subscribed):
        _modelEvaluationService.Unsubscribe();
        _logger.LogInformation("Prediction Build Service stopped at: {time}", DateTimeOffset.Now);
    }
}
