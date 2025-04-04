namespace PredictionBuildService.core.Interfaces;

public interface IBlobStorageMonitorService
{
    Task MonitorAsync(CancellationToken token);
    Task<List<string>> ListBlobsAsync(string containerName, CancellationToken token);
}