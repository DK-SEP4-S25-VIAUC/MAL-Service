namespace Sep4.PredictionApp.Interfaces;

public interface IBlobDownloader
{
    Task DownloadAsync(string uri, string destinationPath);
}