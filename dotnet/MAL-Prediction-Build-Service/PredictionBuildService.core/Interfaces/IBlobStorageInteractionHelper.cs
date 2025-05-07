using Azure.Storage.Blobs;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.core.Interfaces;

/// <summary>
/// Interface for the AzureBlobStorage Helper class that handles conversions between BlobStorage format and Application formats. Exposes relevant helper methods.
/// </summary>
public interface IBlobStorageInteractionHelper
{
    /// <summary>
    /// Reads all prediction models in the specified Azure BlobStorage container, and loads them into the in-memory model cache.
    /// </summary>
    /// <param name="blobServiceClient">The Azure BlobServiceClient that contains the required access information for this BlobStorage.</param>
    /// <param name="token">A CancellationToken that can be used to terminate/end this methods execution prematurely.</param>
    /// <param name="containerName">The name of the container inside the specified BlobStorage, from where filenames should be extracted.</param>
    /// <param name="modelMetaDataFormat">The format the metadata file is in (i.e. model.metadata.json)</param>
    /// <param name="modelFormat">The format the model is in (i.e. model.onnx)</param>
    /// <exception cref="JsonException">Thrown if any deserialization issues occur while converting metadata to model dto.</exception>
    /// <exception cref="Exception">Thrown if any other problems occur during method execution.</exception>
    Task LoadAllModelsIntoCacheAsync(BlobServiceClient blobServiceClient, CancellationToken token, string containerName, string modelMetaDataFormat, string modelFormat);
    
    
    /// <summary>
    /// Lists the filenames for all the blobs inside the Azure BlobStorage specified Blob Container associated with the Azure account specified in the BlobServiceClient.
    /// </summary>
    /// <param name="blobServiceClient">The Azure BlobServiceClient that contains the required access information for this BlobStorage.</param>
    /// <param name="containerName">The name of the container inside the specified BlobStorage, from where filenames should be extracted.</param>
    /// <param name="token">A CancellationToken that can be used to terminate/end this methods execution prematurely.</param>
    /// <returns>A list of all the filenames inside this blob container.</returns>
    /// <exception cref="Exception">Thrown if any problems occur while attempting to read filenames from the provided BlobStorage container.</exception>
    Task<List<string>> ListAllBlobsAsync(BlobServiceClient blobServiceClient, string containerName, CancellationToken token);
    
    
    /// <summary>
    /// Converts and builds a ModelDTO for the specific model, based on the provided jsonMetaData.
    /// </summary>
    /// <param name="jsonMetaData">the json formatted model metadata that should be loaded into an appropriate ModelDTO</param>
    /// <param name="modelMetaDataFormat">The format the metadata file is in (i.e. model.metadata.json)</param>
    /// <param name="modelFormat">The format the model is in (i.e. model.onnx)</param>
    /// <param name="blobClient">The BlobClient containing access information to the Blob Storage, as well as the blob container name and blob name to download.</param>
    /// <returns>
    /// The converted model as a ModelDTO, specific for the type of prediction model.
    /// </returns>
    /// <exception cref="MissingFieldException">Thrown if extracting the model_type information from the provided json fails.</exception>
    /// <exception cref="FormatException">Thrown if the provided json metadata describes a prediction type that the application does not know how to handle.</exception>
    /// <exception cref="JsonException">Thrown if conversion from metadata.json to proper C# class fails.</exception>
    ModelDTO ConvertFromJsonMetadataToModelDTO(string jsonMetaData, string modelMetaDataFormat, string modelFormat, BlobClient blobClient);
    
    
    /// <summary>
    /// Downloads a specified blob from Azure BlobStorage.
    /// The specific blob to download is specified within the BlobClient.
    /// </summary>
    /// <param name="blobClient">The BlobClient containing access information to the Blob Storage, as well as the blob container name and blob name to download.</param>
    /// <param name="token">A cancellation token, that can be used to terminate/cancel this operation.</param>
    /// <returns>The downloaded blob, converted to json inside a string.</returns>
    Task<string> DownloadBlobToStringAsync(BlobClient blobClient, CancellationToken token);
}