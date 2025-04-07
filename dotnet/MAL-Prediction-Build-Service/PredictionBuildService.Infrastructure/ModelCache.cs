using System.Collections.Concurrent;
using PredictionBuildService.core;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure;

public class ModelCache : IModelCache
{
    private ConcurrentDictionary<string, ModelDTO> _modelCache = new();
    
    // Define the event using EventHandler<T>
    public event EventHandler<ModelAddedEventArgs> ModelAdded;

    public async Task<bool> AddModelAsync(ModelDTO newModelDto) {
        return await Task.Run(() => {
            // TODO: Wrap this in a try-catch clause?
            
            // Validate parameters/arguments!
            if (newModelDto == null) {
                throw new ArgumentNullException();
            }
            
            // TODO: Validate the model further??
        
            // Generate key for mapping into the cache:
            var key = GenerateKey(newModelDto.Type, newModelDto.Version);
        
            bool result = _modelCache.TryAdd(key, newModelDto);
            
            // Notify subscribers:
            if (result) {
                OnModelAdded(newModelDto);
            }
            
            // return result:
            return result;
        });
    }

    public async Task<bool> RemoveModelAsync(string modelName) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public async Task<bool> UpdateModelAsync(ModelDTO oldModelDto, ModelDTO newModelDto) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<ModelDTO> ListModelsAsync() {
        // Make a copy of the cache, to ensure no changes are made during iteration (thread-safety):
        var models = _modelCache.Values.ToList();
        
        foreach (var model in models) {
            yield return model;
        }
    }

    public async Task<ModelDTO> FindModelAsync(string type, string version) {
        // TODO: Implement
        throw new NotImplementedException();
    }

    public int CacheSize() {
        return _modelCache.Count;
    }

    protected virtual void OnModelAdded(ModelDTO model) {
        ModelAdded.Invoke(this, new ModelAddedEventArgs(model));
    }

    private string GenerateKey(string type, string version) {
        // TODO: Add validation to ensure type and version are valid! (i.e. not null)!
        return $"{type}-{version}";
    }
}