using System.Text.Json;
using Azure.Messaging.EventGrid;
using PredictionBuildService.core.Interfaces;
using Azure.Storage.Queues;
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
/// </remarks>
public class BlobStorageMonitorServiceImpl : IBlobStorageMonitorService
{
    private readonly ILogger<BlobStorageMonitorServiceImpl> _logger;
    private readonly QueueClient _queueClient;
    private readonly IModelEvaluationService _modelEvaluationService;
    private readonly AzureBlobStorageSettings _settings;

    public BlobStorageMonitorServiceImpl(
        ILogger<BlobStorageMonitorServiceImpl> logger, 
        IModelEvaluationService modelEvaluationService,
        IOptions<AzureBlobStorageSettings> settings
        ) {
        _logger = logger;
        _queueClient = new QueueClient();
        _modelEvaluationService = modelEvaluationService;
    }
    
    
    public async Task MonitorAsync(CancellationToken token) {
        _logger.LogInformation("Blob Storage Monitoring Service started at: {time}", DateTimeOffset.Now);
        
        // Start the monitoring loop:
        while (!token.IsCancellationRequested) {
            
            // Wait to receive a message from the Azure Event Grid:
            var msg = await _queueClient.ReceiveMessageAsync();
            if (msg != null) {
                var eventGridEvent = JsonSerializer.Deserialize<EventGridEvent[]>(msg.Message).FirstOrDefault();
                
                if (eventGridEvent?.EventType == "Microsoft.Storage.BlobCreated") {
                    string blobUri = eventGridEvent.Data["url"].ToString();
                    await _modelEvaluationService(blobUri);
                }
                
                // Delete the message after processing
                await _queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            }
            
            // Wait a little while, to save system resources:
            await Task.Delay(5000, token);
        }
    }
}