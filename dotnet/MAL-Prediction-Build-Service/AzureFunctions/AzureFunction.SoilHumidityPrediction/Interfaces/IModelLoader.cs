namespace Sep4.PredictionApp.Interfaces;

/// <summary>
/// Interface for the ModelLoader Helper class that handles loading a specified prediction model from Azure Blob Storage.
/// </summary>
public interface IModelLoader
{
    /// <summary>
    /// Loads the ONNX model from Azure Blob Storage or returns the cached instance if already loaded.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="IInferenceSession"/>
    /// object representing the loaded ONNX model.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <c>OnnxModelUri</c> environment variable is not set.
    /// </exception>
    /// <exception cref="Azure.RequestFailedException">
    /// Thrown if the ONNX model cannot be downloaded from Azure Blob Storage.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if there is an error writing the model file to the temporary path.
    /// </exception>
    /// <remarks>
    /// The ONNX model is cached locally in the Function App to avoid reloading it for each function invocation.
    /// The model is reloaded if the <c>OnnxModelUri</c> environment variable changes.
    /// </remarks>
    Task<IInferenceSession> GetOrLoadModelAsync();
}