using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.Infrastructure;

/// <summary>
/// Implementation of the AzureBlobStorage Helper class that handles conversions between BlobStorage format and Application formats. Exposes relevant helper methods.
/// </summary>
/// <remarks>
/// Implementation logic is based on available Microsoft tutorials, i.e.:<br />
/// https://learn.microsoft.com/en-us/azure/storage/blobs/ <br />
/// </remarks>
public class BlobStorageInteractionHelperImpl : IBlobStorageInteractionHelper
{
    private readonly ILogger<BlobStorageInteractionHelperImpl> _logger;
    private readonly IModelCache _modelCache;

    /// <summary>
    /// Primary constructor. It is recommended to use dependency injection to inject the specified arguments, instead of manual injection.
    /// </summary>
    /// <param name="logger">A logging service, that can handle logging of messages</param>
    /// <param name="modelCache">The local in-memory cache that holds all currently registered prediction models.</param>
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
                try {
                    if (blobName.Contains(modelMetaDataFormat)) {
                        // Create the client connection to interact with this specific azure blob:
                        BlobClient blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName);

                        // Download the metadata file from Azure Blob Storage:
                        string jsonMetaData = await DownloadBlobToStringAsync(blobClient, cancellationToken);
                        
                        // Convert from json to ModelDTO:
                        var model = ConvertFromJsonMetadataToModelDTO(jsonMetaData, modelMetaDataFormat, modelFormat, blobClient);
                        _logger.LogInformation("Downloaded metadata for model: {blobName}, metadata is: \n{metadata}", blobName, model.ToString());
                        
                        // Add this new ModelDTO to the ModelCache:
                        await _modelCache.AddModelAsync(model);
                    }
                } catch (JsonException jx) {
                    _logger.LogError(jx, "Could not deserialize into ModelDTO: {blobName}", blobName);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error occurred while downloading: {blobName}", blobName);
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
    

    public ModelDTO ConvertFromJsonMetadataToModelDTO(string jsonMetaData, string modelMetaDataFormat, string modelFormat, BlobClient blobClient) {
        
        // Extract model_type from the given json metadata, if it exists:
        JObject obj = JObject.Parse(jsonMetaData);
        string? modelType = (string?) obj["model_type"];
        if (modelType == null) {
            _logger.LogError("In method ConvertFromJsonMetadataToModelDTO(), 'model_type' is not defined in the provided json metadata.");
            throw new MissingFieldException("model_type is not defined in the provided json metadata.");
        }
        
        // Convert to proper model dto:
        ModelDTO? model;
        switch (modelType.ToLower()) {
            // TODO: If other model type are added in the future, ensure to add them below in this switch for proper deserialization during model load.
            
            case "ridge (linear)":
                // Convert json metadata to model dto:
                model = JsonConvert.DeserializeObject<LinearRegressionModelDTO>(jsonMetaData);
                
                // Validate the created dto:
                try {
                    if (model == null) {
                        throw new JsonException("json metadata was deserialized into null object.");
                    }
                    
                    // Fill out common, non-serialized fields:
                    model.DownloadUrl = ConvertMetaDataUriToModelUri(blobClient.Uri, modelMetaDataFormat, modelFormat);
                    
                    // Validate model contents:
                    model.ValidateSelf();
                    
                } catch (Exception ex) {
                    _logger.LogError("In method ConvertFromJsonMetadataToModelDTO(), could not convert LinearRegression model metadata into proper DTO.\nCause: {}", ex.Message);
                    throw new JsonException($"Could not convert LinearRegression model metadata into proper DTO\nCause: {ex.Message}");
                }
                break;
            
            // TODO: Add other possible model types below (i.e. RandomForestDTO).
            
            default:
                _logger.LogError("In method ConvertFromJsonMetadataToModelDTO(), 'model_type' = {modelType} is not a recognized/implemented model type.", modelType);
                throw new FormatException("'model_type' is unrecognized. Unable to continue.");
        }
        
        return model;
    }
    
    
    public async Task<string> DownloadBlobToStringAsync(BlobClient blobClient, CancellationToken token) {
        BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync(token);
        return downloadResult.Content.ToString();
    }

    
    /// <summary>
    /// Class specific method, that handles converting the Uri from the model Metadata, to the Uri for the actual model.
    /// Since the Metadata itself does not contain a Uri reference to each Model, this is necessary - and requires that
    /// models and corresponding metadata adhere to naming conventions.
    /// </summary>
    /// <param name="metadataUri">Uri that points to the Metadata file for the specified model (i.e. http://example.com/model.metadata.json)</param>
    /// <param name="metadataFormat">The format that the metadata is in (i.e. model.metadata.json)</param>
    /// <param name="modelFormat">The format that the model is in (i.e. model.onnx)</param>
    /// <returns>A complete Uri pointing to the model located in the same place as the metadata (i.e. http://example.com/model.onnx)</returns>
    private static string ConvertMetaDataUriToModelUri(Uri metadataUri, string metadataFormat, string modelFormat) {
        
        // Extract the download Uri for the metadata file:
        var uri = metadataUri.AbsoluteUri;
        
        // Replate 'metadata' parts to point to the actual model:
        return uri.Replace(metadataFormat, modelFormat);
    }
}