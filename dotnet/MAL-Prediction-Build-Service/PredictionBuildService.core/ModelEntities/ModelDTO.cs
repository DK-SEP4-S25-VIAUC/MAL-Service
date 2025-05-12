using Newtonsoft.Json;

namespace PredictionBuildService.core.ModelEntities;

[JsonObject]
public abstract class ModelDTO
{
    // Injected variables through JSON deserialization:
    [JsonProperty("model_type")]
    public string? Type { get; set; }
    
    [JsonProperty("training_timestamp_utc")]
    public string? TrainingTimestamp { get; set; }
    
    [JsonProperty("target")]
    public string? Target { get; set; }
    
    [JsonProperty("feature_names")]
    public string[]? FeatureNames { get; set; }
    
    
    // Programmatically set variables:
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Performs self-validation, ensuring that all model metadata values are properly set.
    /// </summary>
    /// <returns></returns>
    public abstract bool ValidateSelf();

    public abstract ModelDTO Copy();
    
    
    public override string ToString() {
        return (
            "Type: " + Type + 
            "\n, Training_timestamp_utc: " + TrainingTimestamp + 
            "\n, DownloadUrl: " + DownloadUrl +
            "\n, target: " + Target + 
            "\n, feature_names: " + FeatureNames
            );
    }
};