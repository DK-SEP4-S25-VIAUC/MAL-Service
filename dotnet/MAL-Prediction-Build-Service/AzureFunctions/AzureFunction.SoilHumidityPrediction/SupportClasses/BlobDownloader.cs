using Azure.Identity;
using Azure.Storage.Blobs;
using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class BlobDownloader : IBlobDownloader
{
    public async Task DownloadAsync(string uri, string destinationPath) {
        var client = new BlobClient(new Uri(uri), new DefaultAzureCredential());
        await client.DownloadToAsync(destinationPath);
    }
}