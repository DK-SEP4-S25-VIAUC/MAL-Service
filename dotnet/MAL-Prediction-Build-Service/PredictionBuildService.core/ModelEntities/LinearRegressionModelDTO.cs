using Newtonsoft.Json;

namespace PredictionBuildService.core.ModelEntities;

[JsonObject]
public class LinearRegressionModelDTO : ModelDTO
{
    // Injected variables through JSON deserialization:
    [JsonProperty("alpha")]
    public string? Alpha { get; set; }
    
    [JsonProperty("cross_val_splits")]
    public string? CrossValSplits { get; set; }
    
    [JsonProperty("rmse_cv")]
    public string? RmseCv { get; set; }
    
    [JsonProperty("r2_insample")]
    public string? R2 { get; set; }
    
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