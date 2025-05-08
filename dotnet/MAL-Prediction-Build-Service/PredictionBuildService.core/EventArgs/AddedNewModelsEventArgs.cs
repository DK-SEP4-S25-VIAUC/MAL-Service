namespace PredictionBuildService.core.EventArgs;

/// <summary>
/// A basic EventArg that is used to provide Observer pattern integration in the application.
/// This class is intended to be used to signal that a new prediction model was added to the BlobStorage.
/// </summary>
public class AddedNewModelsEventArgs : System.EventArgs
{
    // Class is deliberately empty.
}