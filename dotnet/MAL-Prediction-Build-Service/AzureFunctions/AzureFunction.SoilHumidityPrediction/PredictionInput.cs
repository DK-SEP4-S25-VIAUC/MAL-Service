using System.Text.Json.Serialization;

namespace Sep4.PredictionApp;

/// <summary>
/// Represents the input data structure for the SoilHumidityPrediction Azure Function.
/// This class is used to deserialize the JSON request body into a structured format
/// that contains the input parameters required for soil humidity prediction.
/// </summary>
public class PredictionInput
{
    /// <summary>
    /// Gets or sets the dictionary of input parameters for the prediction model.
    /// The dictionary must contain a key named "target" with a float array value
    /// representing the soil humidity value(s) to predict.
    /// This value represent the target soil humidity threshold to find how long it will take to reach
    /// (i.e. how long till we reach 45.7% soil humidity at current rate of evaporation)
    /// </summary>
    /// <remarks>
    /// The "target" value is expected to be a float array with exactly one element
    /// (e.g., [45.7]) to match the ONNX model's input requirements (float32[1]).
    /// The property name in the JSON request body must be "inputs" (lowercase),
    /// which is mapped to this property using the <see cref="JsonPropertyNameAttribute"/>.
    /// </remarks>
    /// <example>
    /// An example JSON request body:
    /// <code>
    /// {
    ///   "inputs": {
    ///     "target": [45.7]
    ///   }
    /// }
    /// </code>
    /// </example>
    [JsonPropertyName("inputs")]
    public required Dictionary<string, float[]> Inputs { get; set; }
}