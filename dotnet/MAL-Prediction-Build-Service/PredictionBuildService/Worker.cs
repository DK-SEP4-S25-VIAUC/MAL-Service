using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configuration;
using PredictionBuildService.core;
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

    public Worker(
        IBlobStorageMonitorService monitorService,
        ILogger<Worker> logger,
        IOptions<AzureBlobStorageSettings> settings,
        IModelCache modelCache,
        BlobServiceClient blobServiceClient) {
        _monitorService = monitorService;
        _logger = logger;
        _settings = settings.Value;
        _modelCache = modelCache;
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// Executes the monitoring service that tracks the Azure Blob Storage repo for changes. It is also the monitoring
    /// service that fires appropriate actions in other services depending on the registered changes.
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("Prediction Build Service started at: {time}", DateTimeOffset.Now);
        
        // Check if the Model Cache is empty. If so, first read all models available from Azure Blob Storage into the in-memory cache.
        // This is necessary upon service initialization, to ensure existing models are properly loaded:
        if (_modelCache.CacheSize() == 0) {
            await LoadModelsIntoCache(stoppingToken, _settings.ContainerName);
        }

        // ------------ TODO: Below is for debugging --------------
        var models = _modelCache.ListModelsAsync();
        var modelsAsString = "Loaded these models:\n";
        await foreach (var model in models) {
            modelsAsString += "type: " + model.Type + ", version: " + model.Version + "\n";
        }
        _logger.LogInformation("{modelsAsString}", modelsAsString);
        // ------------ TODO: Above is for debugging --------------
        
        // Start the Azure Blob Storage monitoring service, to look for all future changes in the model registry:
        // TODO: Implement the monitoring service properly!
        
        //await _monitorService.MonitorAsync(stoppingToken);
    }


    private async Task LoadModelsIntoCache(CancellationToken token, string containerName) {
        _logger.LogInformation("Loading existing models from Azure Blob Storage into local cache");

        // List all blobs in the "models" container:
        var blobNames = await _monitorService.ListBlobsAsync(containerName, token);

        // Identify all blobs that contain model metadata and load these:
        await Parallel.ForEachAsync(blobNames,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token },
            async (blobName, cancellationToken) => {
                try
                {
                    if (blobName.Contains(_settings.ModelMetaDataFormat)) {
                        // Create the client connection to interact with this specific azure blob:
                        BlobClient blobClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName).GetBlobClient(blobName);

                        // Download the metadata file from Azure Blob Storage:
                        string jsonMetaData = await DownloadBlobToStringAsync(blobClient, cancellationToken);
                        
                        // Convert from json to Model:
                        var model = Newtonsoft.Json.JsonConvert.DeserializeObject<Model>(jsonMetaData);
                        
                        // Fill out non-serialized fields:
                        // TODO: UPDATE THESE LINES AFTER METADATA HAS BEEN VERIFIED!
                        model.DownloadUrl = ConvertMetaDataUriToModelUri(blobClient.Uri);
                        model.TrainingDate = DateTime.Now;
                        
                        _logger.LogInformation("Downloaded metadata for model: {blobName}, metadata is: \n{metadata}", blobName, model.ToString());
                        
                        // Add this new Model to the ModelCache:
                        await _modelCache.AddModelAsync(model);
                    }
                } catch (JsonException jx) {
                    _logger.LogError("Could not deserialize into Model: {blobName}", blobName);
                } catch (Exception ex) {
                    _logger.LogError("Error occured while downloading: {blobName}", blobName);
                }
            });

        _logger.LogInformation("Completed loading existing models into local cache");
    }

    
    private static async Task<string> DownloadBlobToStringAsync(BlobClient blobClient, CancellationToken token) {
        BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync(token);
        return downloadResult.Content.ToString();
    }

    private static string ConvertMetaDataUriToModelUri(Uri metadata) {
        
        // Extract the download Uri for the metadata file:
        var metadataUri = metadata.AbsoluteUri;
        
        // Replate 'metadata' parts to point to the actual model:
        return metadataUri.Replace(".metadata.json", ".onnx");
    }
}
