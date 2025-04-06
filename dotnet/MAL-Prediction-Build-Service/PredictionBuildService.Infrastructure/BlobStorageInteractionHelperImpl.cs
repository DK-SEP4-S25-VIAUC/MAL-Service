using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PredictionBuildService.core;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure;

public class BlobStorageInteractionHelperImpl : IBlobStorageInteractionHelper
{
    private readonly ILogger<BlobStorageInteractionHelperImpl> _logger;
    private readonly IModelCache _modelCache;

    public BlobStorageInteractionHelperImpl(
        ILogger<BlobStorageInteractionHelperImpl> logger,
        IModelCache modelCache) {
        _logger = logger;
        _modelCache = modelCache;
    }

    public async Task LoadAllModelsIntoCacheAsync(BlobServiceClient blobServiceClient, CancellationToken token, string containerName, string modelMetaDataFormat, string modelFormat) {
        _logger.LogInformation("Loading existing models from Azure Blob Storage into local cache");

        // List all blobs in the specified container (i.e. 'models'):
        var blobNames = await ListAllBlobsAsync(blobServiceClient, containerName, token);

        // Identify all blobs that contain model metadata and load these:
        await Parallel.ForEachAsync(blobNames,
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = token },
            async (blobName, cancellationToken) => {
                try
                {
                    if (blobName.Contains(modelMetaDataFormat)) {
                        // Create the client connection to interact with this specific azure blob:
                        BlobClient blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

                        // Download the metadata file from Azure Blob Storage:
                        string jsonMetaData = await DownloadBlobToStringAsync(blobClient, cancellationToken);
                        
                        // Convert from json to Model:
                        var model = JsonConvert.DeserializeObject<Model>(jsonMetaData);
                        
                        // Fill out non-serialized fields:
                        // TODO: UPDATE THESE LINES AFTER METADATA HAS BEEN VERIFIED!
                        model.DownloadUrl = ConvertMetaDataUriToModelUri(blobClient.Uri, modelMetaDataFormat, modelFormat);
                        model.TrainingDate = DateTime.Now;
                        
                        _logger.LogInformation("Downloaded metadata for model: {blobName}, metadata is: \n{metadata}", blobName, model.ToString());
                        
                        // Add this new Model to the ModelCache:
                        await _modelCache.AddModelAsync(model);
                    }
                } catch (JsonException jx) {
                    _logger.LogError(jx, "Could not deserialize into Model: {blobName}", blobName);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error occured while downloading: {blobName}", blobName);
                }
            });

        _logger.LogInformation("Completed loading existing models into local cache");
    }

    public async Task<List<string>> ListAllBlobsAsync(BlobServiceClient blobServiceClient, string containerName, CancellationToken token) {
        _logger.LogInformation("Listing blobs in container: {ContainerName}", containerName);

        try {
            // Get a reference to the container
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure the container exists
            await containerClient.CreateIfNotExistsAsync(cancellationToken: token);

            // List blobs in the container
            var blobNames = new List<string>();
            
            await foreach (var blobItem in containerClient.GetBlobsAsync(cancellationToken: token)) {
                blobNames.Add(blobItem.Name);
            }

            _logger.LogInformation("Found {count} blobs\n: {blobNames}", blobNames.Count, blobNames);

            return blobNames;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error listing blobs in container: {ContainerName}", containerName);
            throw;
        }
    }
    
    
    private static async Task<string> DownloadBlobToStringAsync(BlobClient blobClient, CancellationToken token) {
        BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync(token);
        return downloadResult.Content.ToString();
    }

    
    private static string ConvertMetaDataUriToModelUri(Uri metadataUri, string metadataFormat, string modelFormat) {
        
        // Extract the download Uri for the metadata file:
        var uri = metadataUri.AbsoluteUri;
        
        // Replate 'metadata' parts to point to the actual model:
        return uri.Replace(metadataFormat, modelFormat);
    }
}