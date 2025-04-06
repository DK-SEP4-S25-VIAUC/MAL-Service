using PredictionBuildService.core.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
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
    private readonly QueueClient _queueClient;
    private readonly IModelEvaluationService _modelEvaluationService;

    public BlobStorageMonitorServiceImpl(
        ILogger<BlobStorageMonitorServiceImpl> logger, 
        BlobServiceClient blobServiceClient,
        QueueClient queueClient,
        IModelEvaluationService modelEvaluationService) {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
        _queueClient = queueClient;
        _modelEvaluationService = modelEvaluationService;
    }
    
    
    public async Task MonitorAsync(CancellationToken token) {
        /*while (!token.IsCancellationRequested) {
            var msg = await _queueClient.ReceiveMessageAsync();
            if (msg != null) {
                var eventGridEvent = JsonSerializer.Deserialize<EventGridEvent[]>(msg.MessageText).FirstOrDefault();
                if (eventGridEvent?.EventType == "Microsoft.Storage.BlobCreated") {
                    string blobUri = eventGridEvent.Data["url"].ToString();
                    await _modelEvaluationService(blobUri);
                }
                
                // Delete the message after processing
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            }
            await Task.Delay(1000, token); // Avoid tight loop
        }*/
    }

    /*public async Task<List<string>> ListAllBlobsAsync(string containerName, CancellationToken token) {
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
    }*/
}