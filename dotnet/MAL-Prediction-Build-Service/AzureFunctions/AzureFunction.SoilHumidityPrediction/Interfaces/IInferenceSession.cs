using Microsoft.ML.OnnxRuntime;

namespace Sep4.PredictionApp.Interfaces;

/// <summary>
/// A wrapper helper class that provides a layer between the actual IInferenceSession interface and this application,
/// allowing for better control during testing and dependency injection.
/// </summary>
public interface IInferenceSession : IDisposable
{
    /// <summary>
    /// Executes inference using the associated .onnx model via the current <see cref="InferenceSession"/>.
    /// </summary>
    /// <param name="inputs">A collection of input tensors provided to the model for prediction.</param>
    /// <returns>A collection of output results produced by the model.</returns>
    /// <remarks>
    /// To extract a single prediction value from the result:
    /// <code>
    /// var predictionValue = results.First().AsTensor&lt;float&gt;().ToArray()[0];
    /// </code>
    /// </remarks>
    IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Run(IReadOnlyCollection<NamedOnnxValue> inputs);
}