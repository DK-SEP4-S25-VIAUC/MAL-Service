using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PredictionBuildService.core.Interfaces;

public interface IBlobStorageInteractionHelper
{
    Task LoadAllModelsIntoCacheAsync(BlobServiceClient blobServiceClient, CancellationToken token, string containerName, string modelMetaDataFormat, string modelFormat);
    Task<List<string>> ListAllBlobsAsync(BlobServiceClient blobServiceClient, string containerName, CancellationToken token);
    Task<string> DownloadBlobToStringAsync(BlobClient blobClient, CancellationToken token);
    ModelDTO ConvertFromJsonMetadataToModelDTO(string jsonMetaData, string modelMetaDataFormat, string modelFormat, BlobClient blobClient);
}