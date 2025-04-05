using PredictionBuildService.core.Interfaces;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace PredictionBuildService.Infrastructure;

/// <summary>
/// ...
/// </summary>
/// <remarks>
/// Implmentation logic is based on available Microsoft tutorials, i.e.:<br />
/// https://learn.microsoft.com/en-us/azure/storage/blobs/ <br />
/// </remarks>
public class BlobStorageMonitorServiceImpl : IBlobStorageMonitorService
{
    private readonly ILogger<BlobStorageMonitorServiceImpl> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageMonitorServiceImpl(ILogger<BlobStorageMonitorServiceImpl> logger, BlobServiceClient blobServiceClient) {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }
    
    
    public Task MonitorAsync(CancellationToken token) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public async Task<List<string>> ListBlobsAsync(string containerName, CancellationToken token) {
        _logger.LogInformation("Listing blobs in container: {ContainerName}", containerName);

        try {
            // Get a reference to the container
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

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
}