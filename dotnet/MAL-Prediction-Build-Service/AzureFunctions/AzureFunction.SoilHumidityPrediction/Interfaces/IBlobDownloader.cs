namespace Sep4.PredictionApp.Interfaces;

/// <summary>
/// Defines a method for downloading blobs from a remote URI to a local file path.
/// </summary>
public interface IBlobDownloader
{
    /// <summary>
    /// Downloads an Azure Blob Storage hosted file from the specified URI to the given destination path.
    /// </summary>
    /// <param name="uri">The URI of the file to download.</param>
    /// <param name="destinationPath">The local file system path where the file should be saved.</param>
    Task DownloadAsync(string uri, string destinationPath);
}