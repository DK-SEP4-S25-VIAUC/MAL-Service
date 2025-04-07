namespace PredictionBuildService.core.EventArgs;

public class ModelAddedEventArgs : System.EventArgs
{
    public ModelDTO Model { get; }

    public ModelAddedEventArgs(ModelDTO model) {
       Model = model;
    }
}