namespace PredictionBuildService.core.Interfaces;

public interface IEventSubscriber
{
    void Subscribe();
    void Unsubscribe();
    Task HandleEventAsync(object sender, System.EventArgs e);
}