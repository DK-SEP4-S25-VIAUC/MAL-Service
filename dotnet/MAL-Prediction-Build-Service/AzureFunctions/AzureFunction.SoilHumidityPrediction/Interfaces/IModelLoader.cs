namespace Sep4.PredictionApp.Interfaces;

public interface IModelLoader
{
    Task<IInferenceSession> GetOrLoadModelAsync();
}