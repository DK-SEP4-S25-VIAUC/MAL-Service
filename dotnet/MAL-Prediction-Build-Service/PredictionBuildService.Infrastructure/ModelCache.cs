using System.Collections.Concurrent;
using PredictionBuildService.core;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure;

public class ModelCache : IModelCache
{
    private ConcurrentDictionary<string, Model> _modelCache = new();

    public async Task<bool> AddModelAsync(Model newModel) {
        return await Task.Run(() => {
            // TODO: Wrap this in a try-catch clause?
            
            // Validate parameters/arguments!
            if (newModel == null) {
                throw new ArgumentNullException();
            }
            
            // TODO: Validate the model further??
        
            // Generate key for mapping into the cache:
            var key = GenerateKey(newModel.Type, newModel.Version);
        
            // return result:
            return _modelCache.TryAdd(key, newModel);
        });
    }

    public async Task<bool> RemoveModelAsync(string modelName) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public async Task<bool> UpdateModelAsync(Model oldModel, Model newModel) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<Model> ListModelsAsync() {
        // Make a copy of the cache, to ensure no changes are made during iteration (thread-safety):
        var models = _modelCache.Values.ToList();
        
        foreach (var model in models) {
            yield return model;
        }
    }

    public async Task<Model> FindModelAsync(string type, string version) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public int CacheSize() {
        return _modelCache.Count;
    }

    private string GenerateKey(string type, string version) {
        // TODO: Add validation to ensure type and version are valid! (i.e. not null)!
        return $"{type}-{version}";
    }
}