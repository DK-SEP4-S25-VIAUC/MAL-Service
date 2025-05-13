using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class ModelSessionFactory : IModelSessionFactory
{
    public IInferenceSession Create(string modelPath) => new InferenceSessionWrapper(modelPath);
}