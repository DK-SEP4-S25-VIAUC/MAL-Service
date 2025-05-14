namespace Sep4.PredictionApp.Interfaces;

/// <summary>
/// Provides functionality for retrieving environment variables used in the application.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Retrieves the value of the specified environment variable.
    /// </summary>
    /// <param name="variable">The name of the environment variable to retrieve.</param>
    /// <returns>The value of the environment variable, or <c>null</c> if it is not found.</returns>
    string? GetEnvironmentVariable(string variable);
    
    /// <summary>
    /// Gets the environment variable name that stores the URI for the best-performing ONNX soil prediction model.
    /// </summary>
    /// <returns>The name of the environment variable containing the ONNX model URI.</returns>
    string GetEnvVarNameForBestOnnxSoilPredictionModelUri();
}