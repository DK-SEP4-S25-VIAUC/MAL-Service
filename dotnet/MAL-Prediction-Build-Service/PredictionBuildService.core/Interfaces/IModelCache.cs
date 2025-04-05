namespace PredictionBuildService.core.Interfaces;

public interface IModelCache
{
    Task<bool> AddModelAsync(Model newModel);
    Task<bool> RemoveModelAsync(string modelName);
    Task<bool> UpdateModelAsync(Model oldModel, Model newModel);
    IAsyncEnumerable<Model> ListModelsAsync();
    Task<Model> FindModelAsync(string type, string version);
    int CacheSize();
}