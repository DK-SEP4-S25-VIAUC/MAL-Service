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

    
    // TODO Tests?
    // What if conversion to json fails?
    // What if download fails?
    // What if download takes forever?
    // What if there are zero models in the blob?
    // What if containerName is rubbish?
    // What if modelFormat is rubbish?
    // What if modelMetaDataFormat is rubbish?
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
                        
                        // Convert from json to ModelDTO:
                        var model = ConvertFromJsonMetadataToModelDTO(jsonMetaData, modelMetaDataFormat, modelFormat, blobClient);
                        _logger.LogInformation("Downloaded metadata for model: {blobName}, metadata is: \n{metadata}", blobName, model.ToString());
                        
                        // Add this new ModelDTO to the ModelCache:
                        await _modelCache.AddModelAsync(model);
                    }
                } catch (JsonException jx) {
                    _logger.LogError(jx, "Could not deserialize into ModelDTO: {blobName}", blobName);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error occured while downloading: {blobName}", blobName);
                }
            });

        _logger.LogInformation("Completed loading existing models into local cache");
    }

    
    // TODO Tests:
    // What happens if the container is empty?
    // What happens if the containerName is invalid?
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
    
    
    /// <summary>
    /// Converts a received blob Uri pointing towards a metadata file into a ModelDTO class containing all available model metadata information.
    /// </summary>
    /// <param name="jsonMetaData">The jsonMetadata file contents downloaded from blob storage.</param>
    /// <param name="modelMetaDataFormat">The format that the metadata file is in (i.e. .metadata.json), set in appsettings / environment variable.</param>
    /// <param name="modelFormat">The format that the prediction model file is in (i.e. .onnx), set in appsettings / environment variable.</param>
    /// <param name="blobClient">The Azure BlobServiceClient instance that handles interactions with the BlobStorage on Azure.</param>
    /// <returns>A ModelDTO containing all the model metadata in a class compatible with dotnet.</returns>
    /// <exception cref="MissingFieldException">Thrown if any of the required fields in the metadata is missing (i.e. "model_type", or other, json keys are missing)</exception>
    /// <exception cref="JsonException">Thrown if conversion from json to ModelDTO fails, due to json formatting/content issues.</exception>
    /// <exception cref="FormatException">Thrown if one (or more) of the values associated with each key is unrecognized (i.e. "model_type": "supermanPredictor", where supermanPredictor is not a recognized model type)</exception>
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
                model = JsonConvert.DeserializeObject<LinearRegressionModelDTO>(jsonMetaData);
                if (model == null) {
                    _logger.LogError("In method ConvertFromJsonMetadataToModelDTO(), could not convert LinearRegression model metadata into proper DTO.");
                    throw new JsonException("Could not convert LinearRegression model metadata into proper DTO");
                }
                break;
            
            default:
                _logger.LogError("In method ConvertFromJsonMetadataToModelDTO(), 'model_type' = {modelType} is not a recognized/implemented model type.", modelType);
                throw new FormatException("'model_type' is unrecognized. Unable to continue.");
        }
        
        // Fill out common, non-serialized fields:
        model.DownloadUrl = ConvertMetaDataUriToModelUri(blobClient.Uri, modelMetaDataFormat, modelFormat);
        
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