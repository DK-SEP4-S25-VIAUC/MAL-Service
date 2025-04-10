using System.Text.Json;
using Azure;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;
using PredictionBuildService.core;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Monitoring;

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
    private readonly AzureBlobStorageSettings _blobStorageSettings;
    private readonly IModelCache _modelCache;
    private readonly IBlobStorageInteractionHelper _blobStorageInteractionHelper;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly WorkerSettings _workerSettings;
    
    public event Func<object, AddedNewModelsEventArgs, Task>? NewModelsAdded;
    
    public BlobStorageMonitorServiceImpl(
        ILogger<BlobStorageMonitorServiceImpl> logger, 
        IOptions<AzureBlobStorageSettings> blobStorageSettings,
        QueueClient queueClient,
        IModelCache modelCache,
        IBlobStorageInteractionHelper blobStorageInteractionHelper,
        BlobServiceClient blobServiceClient,
        IOptions<WorkerSettings> workerSettings) {
        _logger = logger;
        _queueClient = queueClient;
        _blobStorageSettings = blobStorageSettings.Value;
        _modelCache = modelCache;
        _blobStorageInteractionHelper = blobStorageInteractionHelper;
        _blobServiceClient = blobServiceClient;
        _workerSettings = workerSettings.Value;
    }
    
    
    // Define Event Handler invokers:
    private async Task OnNewModelsAdded() {
        // Fire the event if there are more than null subscribers.
        if (NewModelsAdded != null) {
            await NewModelsAdded.Invoke(this, new AddedNewModelsEventArgs());
        } else {
            _logger.LogWarning("No subscribers to the NewModelsAdded event.");
        }
    }
    
    
    // Define IBlobStorageMonitorService interface specified methods:
    public async Task MonitorAsync(CancellationToken token) {
        _logger.LogInformation("Blob Storage Monitoring Service started at: {time}", DateTimeOffset.Now);
        
        // Start the monitoring loop:
        // This is designed to check for changes only once an hour to ensure Azure costs to CPU usage remain as low as possible.
        while (!token.IsCancellationRequested) {
            try {
                _logger.LogInformation("Checking for changes in Blob Storage at: {time}", DateTimeOffset.Now);
                // Wait to receive a message from the Azure Storage Queue:
                var response = await _queueClient.ReceiveMessagesAsync(maxMessages: 1, cancellationToken: token);
                if (response?.Value != null && response.Value.Length > 0) {
                    // Theres something in the event queue. Let's handle it.
                    await HandleQueueResponse(response, token);
                    // Check if there are more messages waiting to be processed:
                    bool moreMessagesWaiting = false;
                    try {
                        var peekResponse = await _queueClient.PeekMessagesAsync(maxMessages: 1, cancellationToken: token);
                        moreMessagesWaiting = peekResponse?.Value != null && peekResponse.Value.Length > 0;
                    } catch (Exception ex) {
                        _logger.LogError(ex, "Failed to peek at queue messages");
                    }
                    // Notify event subscribers, if queue is empty (meaning we've processed the last of the potential model changes):
                    if (!moreMessagesWaiting) {
                        _logger.LogInformation("Processed entire queue.");
                        await NotifySubscribersAsync();
                    }
                    // Sleep for a few seconds before processing next event:
                    await Task.Delay(3000, token);
                    
                } else {
                    // Queue is empty...
                    // Sleep for 1 hour (or whatever is set in the settings) to minimize CPU usage:
                    _logger.LogInformation("Finished processing changes at: {time}.\nSleeping for {delay} minutes...", DateTimeOffset.Now, _workerSettings.SleepBetweenChecksInterval);
                    await Task.Delay(TimeSpan.FromHours(int.Parse(_workerSettings.SleepBetweenChecksInterval)), token);
                }
                
            } catch (JsonException ex) {
                _logger.LogError(ex, "Failed to deserialize Event Grid message");
            } catch (Exception ex) {
                if (ex is not TaskCanceledException) {
                    _logger.LogError(ex, "Error processing Blob Storage changes.\nSleeping for 10 minutes before retrying...");
                    await Task.Delay(TimeSpan.FromMinutes(10), token);
                } else {
                    _logger.LogError(ex, "Error processing message from queue");
                }
            }
        }
        _logger.LogInformation("Blob Storage Monitoring Service stopped at: {time}", DateTimeOffset.Now);
    }

    public async Task NotifySubscribersAsync() {
        _logger.LogInformation("Notifying subscribers of changes...");
        await OnNewModelsAdded();
    }


    // Define private methods supporting the above code execution:
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
                if (blobUri.Contains(_blobStorageSettings.ModelMetaDataFormat)) {
                    
                    // Download the model metadata:
                    BlobClient blobClient = _blobServiceClient.GetBlobContainerClient(_blobStorageSettings.ContainerName).GetBlobClient(blobName);
                    var modelMetaData = await _blobStorageInteractionHelper.DownloadBlobToStringAsync(blobClient, token);
                    var model = _blobStorageInteractionHelper.ConvertFromJsonMetadataToModelDTO(modelMetaData, _blobStorageSettings.ModelMetaDataFormat, _blobStorageSettings.ModelFileType, blobClient);
                    
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