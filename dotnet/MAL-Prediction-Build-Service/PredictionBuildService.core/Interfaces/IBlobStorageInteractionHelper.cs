using Azure.Storage.Blobs;

namespace PredictionBuildService.core.Interfaces;

public interface IBlobStorageInteractionHelper
{
    public Task LoadAllModelsIntoCacheAsync(BlobServiceClient blobServiceClient, CancellationToken token, string containerName, string modelMetaDataFormat, string modelFormat);
    public Task<List<string>> ListAllBlobsAsync(BlobServiceClient blobServiceClient, string containerName, CancellationToken token);
}