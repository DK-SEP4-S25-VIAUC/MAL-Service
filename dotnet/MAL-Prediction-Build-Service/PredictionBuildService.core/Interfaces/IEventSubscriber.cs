namespace PredictionBuildService.core.Interfaces;

/// <summary>
/// Interface that defines Event handling for classes that subscribe to observable (publishing) classes.
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribes the implementing class to all relevant events.
    /// </summary>
    void Subscribe();
    
    /// <summary>
    /// Unsubscribes the implementing class from all subscribed events
    /// </summary>
    void Unsubscribe();
    
    
    /// <summary>
    /// Handles any subscribed events, when they are fired.
    /// </summary>
    /// <param name="sender">The object that fired the event</param>
    /// <param name="e">The specific event (EventArgs) that was fired</param>
    Task HandleEventAsync(object sender, System.EventArgs e);
}