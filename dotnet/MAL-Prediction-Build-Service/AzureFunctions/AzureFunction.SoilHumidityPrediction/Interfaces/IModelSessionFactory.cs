namespace Sep4.PredictionApp.Interfaces;

public interface IModelSessionFactory
{
    IInferenceSession Create(string modelPath);
}