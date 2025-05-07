using Azure.Identity;
using Azure.Storage.Blobs;
using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class BlobDownloader : IBlobDownloader
{
    public async Task DownloadAsync(string uri, string destinationPath) {
        try {
            var client = new BlobClient(new Uri(uri), new DefaultAzureCredential());
            var response = await client.DownloadToAsync(destinationPath);

            if (response.Status == 404) {
                throw new FileNotFoundException(
                    $"Failed to locate prediction model. Please check that a valid prediction model exists in accessible storage directory in Azure BlobStorage.\n\nMsg: {response.Content}");
            }

            if (response.IsError) {
                throw new Exception(
                    $"Failed to locate prediction model. Please check that a valid prediction model exists in accessible storage directory in Azure BlobStorage.\n\nMsg: {response.Content}");
            }
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException(ex.Message);
        } catch (Exception ex) {
            throw new Exception(ex.Message);
        }
    }
}