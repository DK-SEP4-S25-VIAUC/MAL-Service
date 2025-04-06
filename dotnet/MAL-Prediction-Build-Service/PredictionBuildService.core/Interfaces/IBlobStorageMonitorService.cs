namespace PredictionBuildService.core.Interfaces;

public interface IBlobStorageMonitorService
{
    Task MonitorAsync(CancellationToken token);
}