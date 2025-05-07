using Newtonsoft.Json;

namespace PredictionBuildService.core.ModelEntities;

[JsonObject]
public class LinearRegressionModelDTO : ModelDTO
{
    // Injected variables through JSON deserialization:
    [JsonProperty("alpha")]
    public double? Alpha { get; set; }
    
    [JsonProperty("cross_val_splits")]
    public int? CrossValSplits { get; set; }
    
    [JsonProperty("rmse_cv")]
    public double? RmseCv { get; set; }
    
    [JsonProperty("r2_insample")]
    public double? R2 { get; set; }
    
    public override string ToString() {
        return (
            "Type: " + Type + 
            ", Training_timestamp_utc: " + TrainingTimestamp + 
            "\n, DownloadUrl: " + DownloadUrl +
            "\n, target: " + Target + 
            "\n, feature_names: " + FeaturesNames +
            "\n, alpha: " + Alpha + 
            "\n, CrossValSplits: " + CrossValSplits + 
            "\n, RmseCV: " + RmseCv + 
            "\n, R2: " + R2
        );
    }
};