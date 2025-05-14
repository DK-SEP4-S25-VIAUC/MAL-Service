using Newtonsoft.Json;

namespace PredictionBuildService.core.ModelEntities;

[JsonObject]
public class RandomForestModelDTO : ModelDTO
{
    [JsonProperty("cross_val_splits")]
    public int? CrossValSplits { get; set; }

    [JsonProperty("rmse_cv")]
    public double? RmseCv { get; set; }

    [JsonProperty("r2_insample")]
    public double? R2 { get; set; }

    public double? ComputedScore { get; set; }

    private readonly string[] _validRandomForestModelTypes = ["RandomForest"];

    public override bool ValidateSelf() {
        if (string.IsNullOrWhiteSpace(Type) || !_validRandomForestModelTypes.Any(type => type.Equals(Type, StringComparison.OrdinalIgnoreCase))) {
            throw new ArgumentException($"Type='{Type}' cannot be null, empty, or invalid. Must be one of: {string.Join(", ", _validRandomForestModelTypes)}.");
        }

        if (string.IsNullOrWhiteSpace(TrainingTimestamp)) {
            throw new ArgumentException($"TrainingTimestamp='{TrainingTimestamp}' cannot be null or empty.");
        }

        try {
            DateTimeOffset.Parse(TrainingTimestamp!);
        } catch {
            throw new ArgumentException($"TrainingTimestamp='{TrainingTimestamp}' could not be parsed. Use ISO8601 format.");
        }

        if (string.IsNullOrWhiteSpace(Target)) {
            throw new ArgumentException("Target cannot be null or empty.");
        }

        if (FeatureNames == null || FeatureNames.Length == 0) {
            throw new ArgumentException("FeatureNames[] cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(DownloadUrl)) {
            throw new ArgumentException("DownloadUrl cannot be null or empty.");
        }

        if (CrossValSplits == null || CrossValSplits < 0) {
            throw new ArgumentException($"CrossValSplits={CrossValSplits} is invalid.");
        }

        if (RmseCv == null || RmseCv < 0) {
            throw new ArgumentException($"RmseCv={RmseCv} is invalid.");
        }

        if (R2 == null || R2 < 0 || R2 > 1) {
            throw new ArgumentException($"R2={R2} is outside valid range [0, 1].");
        }

        return true;
    }

    public override RandomForestModelDTO Copy() {
        var serialized = JsonConvert.SerializeObject(this, new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto
        });

        var copy = JsonConvert.DeserializeObject<RandomForestModelDTO>(serialized, new JsonSerializerSettings {
            TypeNameHandling = TypeNameHandling.Auto
        });

        if (copy == null)
            throw new InvalidOperationException("Failed to copy RandomForestModelDTO.");

        return copy;
    }

    public override string ToString() {
        return (
            "Type: " + Type +
            ", TrainingTimestampUtc: " + TrainingTimestamp +
            "\n, DownloadUrl: " + DownloadUrl +
            "\n, Target: " + Target +
            "\n, FeatureNames: " + FeatureNames +
            "\n, CrossValSplits: " + CrossValSplits +
            "\n, RmseCV: " + RmseCv +
            "\n, R2: " + R2 +
            "\n, ComputedScore: " + ComputedScore
        );
    }
}
