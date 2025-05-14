namespace Sep4.PredictionApp.Interfaces;

/// <summary>
/// Interface for the ModelSessionFactory Helper class that handles creating a session/connection
/// with the loaded onnx prediction model that can be used to make predictions through.
/// </summary>
public interface IModelSessionFactory
{
    /// <summary>
    /// Creates an inference session that can be used to make predictions with the specified
    /// .onnx model located at the specified path.
    /// </summary>
    /// <param name="modelPath"></param>
    /// <returns>A reference to the created session built with the onnx file/model at the specified path.</returns>
    IInferenceSession Create(string modelPath);
}