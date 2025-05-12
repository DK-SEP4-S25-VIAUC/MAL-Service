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
    
    private readonly string[] _validLinearRegressionModelTypes = ["Ridge (linear)"];

    public override bool ValidateSelf() {
        // Validate each parameter individually:
        
        if (string.IsNullOrWhiteSpace(Type) || !_validLinearRegressionModelTypes.Any(type => type.Equals(Type, StringComparison.OrdinalIgnoreCase))) {
            throw new ArgumentException($"Type='{Type}', cannot be Null, Empty or Whitespace.\nMust match one of these values: {string.Join(", ", _validLinearRegressionModelTypes)}.");
        }

        if (string.IsNullOrWhiteSpace(TrainingTimestamp)) {
            throw new ArgumentException($"TrainingTimeStamp='{TrainingTimestamp}', cannot be Null, Empty or Whitespace.");
        }
        
        if(TrainingTimestamp != null) {
            // See if the given string can be converted to a UTC timestamp. If not, return false.
            try {
                DateTimeOffset.Parse(TrainingTimestamp);
            } catch (Exception ignored) {
                throw new ArgumentException($"TrainingTimeStamp='{Type}', Could not be converted to DateTimeOffset. Ensure that it corresponds to ISO8601 standards.");
            }
        }

        if (string.IsNullOrWhiteSpace(Target)) {
            throw new ArgumentException($"Target='{Target}', cannot be Null, Empty or Whitespace.");
        }

        if (FeatureNames == null || FeatureNames.Length == 0) {
            throw new ArgumentException($"FeatureNames[]='{FeatureNames}', cannot be Null or Empty.");
        }

        if (string.IsNullOrWhiteSpace(DownloadUrl)) {
            throw new ArgumentException($"DownloadUrl='{DownloadUrl}', cannot be Null, Empty or Whitespace.");
        }

        if (Alpha == null || Alpha < 0) {
            throw new ArgumentException($"Alpha='{Alpha}', cannot be Null or have a value outside of range [0, {double.MaxValue}].");
        }

        if (CrossValSplits == null || CrossValSplits < 0) {
            throw new ArgumentException($"CrossValSplits='{CrossValSplits}', cannot be Null or have a value outside of range [0, {int.MaxValue}].");
        }

        if (RmseCv == null || RmseCv < 0) {
            throw new ArgumentException($"RmseCv='{RmseCv}', cannot be Null or have a value outside of range [0, {double.MaxValue}].");
        }

        if (R2 == null || R2 < 0 || R2 > 1) {
            throw new ArgumentException($"R2='{R2}', cannot be Null or have a value outside of range [0, {double.MaxValue}].");
        }
        
        // Validated successfully.
        return true;
    }
    
    public override LinearRegressionModelDTO Copy() {
        var serialized = JsonConvert.SerializeObject(this, new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto
        });

        var copy = JsonConvert.DeserializeObject<LinearRegressionModelDTO>(serialized, new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto
        });

        if (copy == null)
            throw new InvalidOperationException("Failed to copy LinearRegressionModelDTO.");

        return copy;
    }

    public override string ToString() {
        return (
            "Type: " + Type + 
            ", Training_timestamp_utc: " + TrainingTimestamp + 
            "\n, DownloadUrl: " + DownloadUrl +
            "\n, target: " + Target + 
            "\n, feature_names: " + FeatureNames +
            "\n, alpha: " + Alpha + 
            "\n, CrossValSplits: " + CrossValSplits + 
            "\n, RmseCV: " + RmseCv + 
            "\n, R2: " + R2
        );
    }
};