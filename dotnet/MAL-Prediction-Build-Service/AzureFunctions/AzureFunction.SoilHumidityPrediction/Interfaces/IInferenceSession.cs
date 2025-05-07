using Microsoft.ML.OnnxRuntime;

namespace Sep4.PredictionApp.Interfaces;

public interface IInferenceSession : IDisposable
{
    IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Run(IReadOnlyCollection<NamedOnnxValue> inputs);
}