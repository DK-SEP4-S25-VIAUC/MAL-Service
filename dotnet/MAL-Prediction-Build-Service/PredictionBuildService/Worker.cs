using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService;

/// <summary>
/// Main worker thread, responsible to instantiating and running all services, as well as responsible for shutting these services down gracefully.
/// </summary>
/// <remarks>
/// https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-download
/// </remarks>
public class Worker : BackgroundService
{
    private readonly IBlobStorageMonitorService _monitorService;
    private readonly ILogger<Worker> _logger;
    private readonly AzureBlobStorageSettings _settings;
    private readonly IModelCache _modelCache;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IBlobStorageInteractionHelper _blobBlobStorageInteractionHelper;
    private readonly IModelEvaluationService _modelEvaluationService;
    private readonly IBuildService _buildService;

    public Worker(
        IBlobStorageMonitorService monitorService,
        ILogger<Worker> logger,
        IOptions<AzureBlobStorageSettings> settings,
        IModelCache modelCache,
        BlobServiceClient blobServiceClient,
        IBlobStorageInteractionHelper blobBlobStorageInteractionHelper,
        IModelEvaluationService modelEvaluationService,
        IBuildService buildService) {
        _monitorService = monitorService;
        _logger = logger;
        _settings = settings.Value;
        _modelCache = modelCache;
        _blobServiceClient = blobServiceClient;
        _blobBlobStorageInteractionHelper = blobBlobStorageInteractionHelper;
        _modelEvaluationService = modelEvaluationService;
        _buildService = buildService;
    }

    /// <summary>
    /// Executes the monitoring service that tracks the Azure Blob Storage repo for changes. It is also the monitoring
    /// service that fires appropriate actions in other services depending on the registered changes.
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("PredictionBuildService started at: {time}", DateTimeOffset.Now);
        
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
            modelsAsString += "type: " + model.Type + ", version: " + model.TrainingTimestamp + "\n";
        }
        _logger.LogInformation("{modelsAsString}", modelsAsString);
        
        // Evaluate loaded models and deploy the best (ensures that a 'best model' is initially ready for use):
        await _monitorService.NotifySubscribersAsync();
        
        // Start the Azure Blob Storage monitoring service, to look for all future changes in the model registry:
        await _monitorService.MonitorAsync(stoppingToken);
        
        // Begin application shutdown (ensure all subscribers are un-subscribed):
        _modelEvaluationService.Unsubscribe();
        _buildService.Unsubscribe();
        _logger.LogInformation("PredictionBuildService stopped at: {time}", DateTimeOffset.Now);
    }
}
