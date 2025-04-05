using Newtonsoft.Json;

namespace PredictionBuildService.core;

[JsonObject]
public class Model
{
    [JsonProperty("model_type")]
    public string? Type { get; set; }            // I.e.: LinearRegression
    
    [JsonProperty("model_version")]
    public string? Version { get; set; }         // I.e.: v1
    
    public string? DownloadUrl { get; set; }    // Is manually set after metadata is loaded.
    public DateTime TrainingDate { get; set; }  // TODO: Evaluate if this data is part of the actual metadata!
    
    [JsonProperty("accuracy")]
    public double AccuracyScore { get; set; }
    
    [JsonProperty("f1_score")]
    public double F1Score { get; set; }
    
    [JsonProperty("recall")]
    public double RecallScore { get; set; }
    
    [JsonProperty("precision")]
    public double PrecisionScore { get; set; }

    public override string ToString() {
        return (
            "Type: " + Type + 
            ", Version: " + Version + 
            ", DownloadUrl: " + DownloadUrl + 
            ", TrainingDate: " + TrainingDate + 
            ", Accuracy: " + AccuracyScore + 
            ", F1-Score: " + F1Score + 
            ", Recall: " + RecallScore + 
            ", Precision: " + PrecisionScore);
    }
};