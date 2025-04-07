namespace PredictionBuildService.core.Interfaces;

public interface EventSubscriber
{
    void Subscribe();
    void Unsubscribe();
    void HandleEvent();
}