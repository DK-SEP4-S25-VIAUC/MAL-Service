using System.Text.Json.Serialization;

namespace Sep4.PredictionApp;

public class PredictionInput
{
    [JsonPropertyName("inputs")]
    public required Dictionary<string, float[]> Inputs { get; set; }
}