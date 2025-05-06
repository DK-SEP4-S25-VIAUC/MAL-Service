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
    public string[]? FeaturesNames { get; set; }
    
    
    // Programmatically set variables:
    public string? DownloadUrl { get; set; }
    
    
    public override string ToString() {
        return (
            "Type: " + Type + 
            ", Training_timestamp_utc: " + TrainingTimestamp + 
            ", DownloadUrl: " + DownloadUrl +
            ", target: " + Target + 
            ", feature_names: " + FeaturesNames
            );
    }
};