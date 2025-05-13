using PredictionBuildService.core.ModelEntities;
using System.Data;

namespace PredictionBuildService.core.Interfaces;

public interface IModelCache
{
    /// <summary>
    /// Adds a Model to the ModelCache
    /// </summary>
    /// <param name="newModelDto">The Model to add to the ModelCache</param>
    /// <returns>True, if model was successfully added. False if not.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any of the arguments is null</exception>
    /// <exception cref="DuplicateNameException">Thrown if the model already exists in the ModelCache.</exception>
    Task<bool> AddModelAsync(ModelDTO newModelDto);
    
    /// <summary>
    /// Removes a model from the ModelCache.
    /// </summary>
    /// <param name="modelType">Model type (i.e. 'LinearRegression')</param>
    /// <param name="modelVersion">Version (i.e. '1.0.0')</param>
    /// <returns>True, if removal was successful. Otherwise, False (i.e. if the Model to remove isn't found in the ModelCache)</returns>
    /// <exception cref="ArgumentException">Thrown if any of the arguments is null</exception>
    Task<bool> RemoveModelAsync(string modelType, string modelVersion);
    
    /// <summary>
    /// Updates an existing model, identified with the specified arguments, if such as model exists in the ModelCache.
    /// </summary>
    /// <param name="oldModelDto">The old model that should be replaced.</param>
    /// <param name="newModelDto">The new model that should replace the old one.</param>
    /// <returns>True if update was successful. False if unsuccessful (i.e. if the oldModelDTO couldn't be found in the ModelCache)</returns>
    /// <exception cref="ArgumentNullException">Thrown if any of the arguments is null.</exception>
    Task<bool> UpdateModelAsync(ModelDTO? oldModelDto, ModelDTO? newModelDto);
    
    /// <summary>
    /// Lists all models that are stored in the ModelCache.
    /// </summary>
    /// <returns>
    /// An AsyncEnumerable List of ModelDTO's, containing all the currently available models in the ModelCache.
    /// </returns>
    IAsyncEnumerable<ModelDTO> ListModelsAsync();
    
    /// <summary>
    /// Searches the ModelCache for any Model with the specified arguments.
    /// </summary>
    /// <param name="type">Model type (i.e. 'LinearRegression')</param>
    /// <param name="version">Version (i.e. '1.0.0')</param>
    /// <returns>The ModelDTO containing the found model, if it exists in the ModelCache.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no model matching the arguments can be found.</exception>
    Task<ModelDTO> FindModelAsync(string? type, string? version);
    
    /// <summary>
    /// Gets the number of Models already in the local cache.
    /// </summary>
    /// <returns>the number of models in the ModelCache as an integer</returns>
    int CacheSize();
}