using Newtonsoft.Json;

namespace PredictionBuildService.core.ModelEntities;

[JsonObject]
public abstract class LinearRegressionModelDTO : ModelDTO
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
};