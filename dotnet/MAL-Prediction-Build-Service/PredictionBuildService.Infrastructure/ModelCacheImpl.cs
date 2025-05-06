using System.Collections.Concurrent;
using System.Data;
using Microsoft.Extensions.Logging;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.Infrastructure;

/// <summary>
/// Implementation of the IModelCache interface that handles the in-memory processing and storage of registered prediction models.
/// </summary>
public class ModelCacheImpl : IModelCache
{
    private ConcurrentDictionary<string, ModelDTO> _modelCache = new();
    private readonly ILogger<ModelCacheImpl> _logger;

    /// <summary>
    /// Primary constructor. It is recommended to use dependency injection to inject the specified arguments, instead of manual injection.
    /// </summary>
    /// <param name="logger">A logging service, that can handle logging of messages</param>
    public ModelCacheImpl(
        ILogger<ModelCacheImpl> logger) {
        _logger = logger;
    }
    
    
    public async Task<bool> AddModelAsync(ModelDTO newModelDto) {
        
        return await Task.Run(() => {
           
            // Validate parameters/arguments!
            if (newModelDto == null) {
                _logger.LogError("In method AddModelAsync(), arg newModelDto={newModelDto}. Cannot be null\n", newModelDto);
                throw new ArgumentNullException(nameof(newModelDto));
            }
        
            // Generate key for mapping into the cache:
            var key = GenerateKey(newModelDto.Type, newModelDto.Version);
            
            // Check if this key position is already occupied:
            if (_modelCache.TryGetValue(key, out ModelDTO? model)) {
                _logger.LogError("In method AddModelAsync(), index (key) position is dictionary is already occupied with a model (duplicate)\n");
                throw new DuplicateNameException();
            }
        
            // return result:
            return _modelCache.TryAdd(key, newModelDto);
        });
    }

    
    public async Task<bool> RemoveModelAsync(string modelType, string modelVersion) {
        return await Task.Run(() => {
            if (string.IsNullOrWhiteSpace(modelType)) {
                _logger.LogError("In method RemoveModelAsync(), arg modelType is null or empty.");
                throw new ArgumentException("modelName cannot be null or empty", nameof(modelType));
            }

            if (string.IsNullOrWhiteSpace(modelVersion)) {
                _logger.LogError("In method RemoveModelAsync(), arg modelVersion is null or empty.");
                throw new ArgumentException("modelVersion cannot be null or empty", nameof(modelVersion));
            }
            
            // Generate key to find and remove:
            var key = GenerateKey(modelType, modelVersion);

            return _modelCache.TryRemove(key, out _);
        });
    }

    
    public async Task<bool> UpdateModelAsync(ModelDTO? oldModelDto, ModelDTO? newModelDto) {
        return await Task.Run(() => {
            // Validate arguments:
            if (oldModelDto == null) {
                _logger.LogError("In method UpdateModelAsync(), arg oldModelDto is null.");
                throw new ArgumentNullException(nameof(oldModelDto));
            }

            if (newModelDto == null) {
                _logger.LogError("In method UpdateModelAsync(), arg newModelDto is null.");
                throw new ArgumentNullException(nameof(newModelDto));
            }

            var oldKey = GenerateKey(oldModelDto.Type, oldModelDto.Version);
            var newKey = GenerateKey(newModelDto.Type, newModelDto.Version);

            // Remove old model (if exists):
            if (_modelCache.TryRemove(oldKey, out _)) {
                return _modelCache.TryAdd(newKey, newModelDto);
            }

            _logger.LogError("In method UpdateModelAsync(), the model to update was not found in cache.");
            return false;
        });
    }
    

    public async IAsyncEnumerable<ModelDTO> ListModelsAsync() {
        // Make a copy of the cache, to ensure no changes are made during iteration (thread-safety):
        var models = _modelCache.Values.ToList();
        
        foreach (var model in models) {
            yield return model;
        }
    }

    
    public async Task<ModelDTO> FindModelAsync(string? type, string? version) {
        return await Task.Run(() => {
            var key = GenerateKey(type, version);

            if (_modelCache.TryGetValue(key, out var model)) {
                return model;
            }

            _logger.LogError("In method FindModelAsync(), model with key {key} not found.", key);
            throw new KeyNotFoundException($"Model with key '{key}' not found.");
        });
    }

    
    public int CacheSize() {
        return _modelCache.Count;
    }
    

    /// <summary>
    /// Generates a key used in determining where in the ModelDTO dictionary this model should be placed.
    /// </summary>
    /// <param name="type">Model type (i.e. 'LinearRegression')</param>
    /// <param name="version">Version (i.e. '1.0.0')</param>
    /// <returns>A key value as a string for this ModelDTO, to be used when adding to the ModelCache</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null</exception>
    /// <exception cref="ArgumentException">Thrown when any argument fails further validation</exception>
    private string GenerateKey(string? type, string? version) {
        // Validation:
        // For Null
        if (type == null) {
            _logger.LogError("In method GenerateKey(), arg type={type}. Cannot be null\n", type);
            throw new ArgumentNullException(nameof(type));
        }

        if (version == null) {
            _logger.LogError("In method GenerateKey(), arg version={version}. Cannot be null\n", version);
            throw new ArgumentNullException(nameof(version));
        }
            
        
        // For empty:
        if (type.Length < 1) {
            _logger.LogError("In method GenerateKey(), arg type={type}. Cannot be empty\n", type);
            throw new ArgumentException("argument cannot be empty", nameof(type));
        }
        
        if (version.Length < 1) {
            _logger.LogError("In method GenerateKey(), arg version={version}. Cannot be empty\n", version);
            throw new ArgumentException("argument cannot be empty", nameof(version));
        }
        
        // Return generated key:
        return $"{type}-{version}";
    }
}