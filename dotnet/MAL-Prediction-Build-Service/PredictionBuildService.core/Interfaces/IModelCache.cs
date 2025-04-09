using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.core.Interfaces;

public interface IModelCache
{
    Task<bool> AddModelAsync(ModelDTO newModelDto);
    Task<bool> RemoveModelAsync(string modelName);
    Task<bool> UpdateModelAsync(ModelDTO oldModelDto, ModelDTO newModelDto);
    IAsyncEnumerable<ModelDTO> ListModelsAsync();
    Task<ModelDTO> FindModelAsync(string type, string version);
    int CacheSize();
}