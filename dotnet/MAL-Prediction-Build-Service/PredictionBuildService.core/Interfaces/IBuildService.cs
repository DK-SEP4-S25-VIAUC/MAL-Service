namespace PredictionBuildService.core.Interfaces;

/// <summary>
/// Interface for the AzureBlobStorage Monitor class that handles monitoring changes that happen in the BlobStorage and firing events based on what occurs.
/// </summary>
public interface IBuildService : IEventSubscriber
{
    // Currently no specific methods for this interface are specified.
}