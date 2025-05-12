using Microsoft.ML.OnnxRuntime;
using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class InferenceSessionWrapper : IInferenceSession
{
    private readonly InferenceSession _session;

    public InferenceSessionWrapper(string modelPath) {
        _session = new InferenceSession(modelPath);
    }
    
    public IDisposableReadOnlyCollection<DisposableNamedOnnxValue> Run(IReadOnlyCollection<NamedOnnxValue> inputs) {
        return _session.Run(inputs);
    }
    
    public void Dispose() {
        _session?.Dispose();
    }
}