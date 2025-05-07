namespace PredictionBuildService.Configurations;

/// <summary>
/// Contains properties for Worker settings, defined in the appsettings.json file.
/// This allows for dependency injection and usage across all classes in the project.
/// </summary>
public class WorkerSettings
{
    public string SleepBetweenChecksInterval { get; set; } = string.Empty;
}