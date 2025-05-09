using PredictionBuildService.core.EventArgs;

namespace PredictionBuildService.core.Interfaces;

/// <summary>
/// Interface for the AzureBlobStorage Monitor class that handles monitoring changes that happen in the BlobStorage and firing events based on what occurs.
/// </summary>
public interface IBlobStorageMonitorService
{
    /// <summary>
    /// Starts a thread that continuously monitors the BlobStorage for changes. It runs until it receives the CancellationToken.
    /// Any exceptions that occur are logged before the thread continues to its next loop.
    /// The service runs in fixed intervals, checking if there are changes in the BlobStorage and if not sleeping/waiting for a time specified within the WorkerSettings class.
    /// </summary>
    /// <param name="token">A CancellationToken that can be used to terminate/end this methods execution prematurely, in a gentle manner.</param>
    Task MonitorAsync(CancellationToken token);
    
    
    /// <summary>
    /// Fires the defined events, so that subscribers are notified to these events.
    /// </summary>
    Task NotifySubscribersAsync();
    
    
    // Events associated with this interface:
    event Func<object, AddedNewModelsEventArgs, Task> NewModelsAdded;
}