using Newtonsoft.Json;

namespace PredictionBuildService.core.ModelEntities;

[JsonObject]
public class ModelDTO
{
    [JsonProperty("model_type")]
    public string? Type { get; set; }            // I.e.: LinearRegression
    
    [JsonProperty("model_version")]
    public string? Version { get; set; }         // I.e.: v1
    
    public string? DownloadUrl { get; set; }    // Is manually set after metadata is loaded.
    public DateTime TrainingDate { get; set; }  // TODO: Evaluate if this data is part of the actual metadata!
    
    public override string ToString() {
        return (
            "Type: " + Type + 
            ", Version: " + Version + 
            ", DownloadUrl: " + DownloadUrl + 
            ", TrainingDate: " + TrainingDate
            );
    }
};