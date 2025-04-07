using PredictionBuildService.core.EventArgs;

namespace PredictionBuildService.core.Interfaces;

public interface IBlobStorageMonitorService
{
    Task MonitorAsync(CancellationToken token);
    event Func<object, AddedNewModelsEventArgs, Task> NewModelsAdded;
}