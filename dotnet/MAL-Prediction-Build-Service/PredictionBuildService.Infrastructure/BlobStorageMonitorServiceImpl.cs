using System.Text.Json;
using Azure;
using Azure.Messaging.EventGrid;
using PredictionBuildService.core.Interfaces;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;

namespace PredictionBuildService.Infrastructure;

/// <summary>
/// ...
/// </summary>
/// <remarks>
/// Implmentation logic is based on available Microsoft tutorials, i.e.:<br />
/// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/messaging.eventgrid-readme?view=azure-dotnet <br />
/// https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme?view=azure-dotnet <br />
/// </remarks>
public class BlobStorageMonitorServiceImpl : IBlobStorageMonitorService
{
    private readonly ILogger<BlobStorageMonitorServiceImpl> _logger;
    private readonly QueueClient _queueClient;
    private readonly AzureBlobStorageSettings _settings;
    private readonly IModelCache _modelCache;
    private readonly IBlobStorageInteractionHelper _blobStorageInteractionHelper;
    private readonly BlobServiceClient _blobServiceClient;
    
    public BlobStorageMonitorServiceImpl(
        ILogger<BlobStorageMonitorServiceImpl> logger, 
        IOptions<AzureBlobStorageSettings> settings,
        QueueClient queueClient,
        IModelCache modelCache,
        IBlobStorageInteractionHelper blobStorageInteractionHelper,
        BlobServiceClient blobServiceClient
        ) {
        _logger = logger;
        _queueClient = queueClient;
        _settings = settings.Value;
        _modelCache = modelCache;
        _blobStorageInteractionHelper = blobStorageInteractionHelper;
        _blobServiceClient = blobServiceClient;
    }
    
    
    public async Task MonitorAsync(CancellationToken token) {
        _logger.LogInformation("Blob Storage Monitoring Service started at: {time}", DateTimeOffset.Now);
        
        // Start the monitoring loop:
        while (!token.IsCancellationRequested) {
            try {
                // Wait to receive a message from the Azure Storage Queue
                var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 1, cancellationToken: token);
                if (response?.Value != null && response.Value.Length > 0) {
                    await HandleQueueResponse(response, token);
                }
            } catch (JsonException ex) {
                _logger.LogError(ex, "Failed to deserialize Event Grid message");
            } catch (Exception ex) {
                _logger.LogError(ex, "Error processing message from queue");
            }
            
            // Wait a little while, to save system resources:
            await Task.Delay(5000, token);
        }
        
        _logger.LogInformation("Blob Storage Monitoring Service stopped at: {time}", DateTimeOffset.Now);
    }

    
    private async Task HandleQueueResponse(Response<QueueMessage[]> response, CancellationToken token) {
        // Pick the first response from the received messages. This is necessary because the method 'ReceiveMessagesAsync'
        // returns an array of messages (delimited by maxMessages).
        var msg = response.Value[0];

        // Deserialize the message body (Event Grid sends an array of events). The body is base64 coded, and must be decoded:
        string decodedReponse;
        try {
            decodedReponse = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(msg.Body.ToString()));
            _logger.LogInformation("Decoded this message from Queue: {body}", decodedReponse);
        } catch (FormatException ex) {
            _logger.LogError(ex, "Queue message is not base64-encoded: {body}", msg.Body.ToString());
            
            // Delete the message to avoid reprocessing
            await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, token);
            return;
        }

        var eventGridEvent = EventGridEvent.Parse(BinaryData.FromString(decodedReponse));

        // Extract the EventType from the grid. We only look for the 'BlobCreated' event - when new models are added to the model registry:
        if (eventGridEvent?.EventType == "Microsoft.Storage.BlobCreated") {
            
            // Deserialize the event data to access the URL:
            var eventData = JsonSerializer.Deserialize<Dictionary<string, object>>(eventGridEvent.Data.ToString());
            string? blobUri = null;
            if (eventData?.ContainsKey("url") == true) {
                blobUri = eventData["url"].ToString();
            }

            // Check if the blob URL contains reference to 'model metadata', if not we simply ignore it. Models without correct metadata are not evaluated nor deployed:
            if (!string.IsNullOrEmpty(blobUri)) {
                
                // Extract the blobName from the Uri (the part after the final '/'):
                string blobName = new Uri(blobUri).Segments.Last();
                
                _logger.LogInformation("Processing blob creation event for URI: {blobUri} with name: {blobName}", blobUri, blobName);
                
                // ModelMetaDataFormat is set in appsettings.json. Maybe it's like '.metadata.json' - maybe some other format:
                if (blobUri.Contains(_settings.ModelMetaDataFormat)) {
                    
                    // Download the model metadata:
                    BlobClient blobClient = _blobServiceClient.GetBlobContainerClient(_settings.ContainerName).GetBlobClient(blobName);
                    var modelMetaData = await _blobStorageInteractionHelper.DownloadBlobToStringAsync(blobClient, token);
                    var model = _blobStorageInteractionHelper.ConvertFromJsonMetadataToModelDTO(modelMetaData, _settings.ModelMetaDataFormat, _settings.ModelFileType, blobClient);
                    
                    // Add the newly found model to the ModelCache:
                    await _modelCache.AddModelAsync(model);
                }
            } else  {
                _logger.LogWarning("Blob URI not found in event data: {eventData}", eventGridEvent.Data.ToString());
            }
        }  else  {
            _logger.LogWarning("Received unexpected event type: {eventType}.\nNo action was performed on this event.", eventGridEvent?.EventType);
        }

        // Delete the message after processing
        await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt, token);
    }
}