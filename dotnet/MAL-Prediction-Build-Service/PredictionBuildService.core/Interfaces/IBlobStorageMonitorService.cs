using PredictionBuildService.core.EventArgs;

namespace PredictionBuildService.core.Interfaces;

public interface IBlobStorageMonitorService
{
    Task MonitorAsync(CancellationToken token);
    Task NotifySubscribersAsync();
    event Func<object, AddedNewModelsEventArgs, Task> NewModelsAdded;
}