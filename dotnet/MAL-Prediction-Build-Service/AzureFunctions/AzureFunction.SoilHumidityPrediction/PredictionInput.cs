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
    /// The dictionary should contain the features required by the specific model with a float array value for each feature.
    /// </summary>
    /// <remarks>
    /// The overall property name in the JSON request body must be "inputs" (lowercase),
    /// which is mapped to this property using the <see cref="JsonPropertyNameAttribute"/>.
    /// </remarks>
    /// <example>
    /// An example JSON request body:
    /// <code>
    /// {
    ///   "inputs": {
    ///     "soil_humidity": [45.7],
    ///     "soil_delta": [45.7],
    ///     "air_humidity": [45.7],
    ///     "temperature": [45.7],
    ///     "light": [45.7],
    ///     "hour_sin": [45.7],
    ///     "hour_cos": [45.7]
    ///   }
    /// }
    /// </code>
    /// </example>
    [JsonPropertyName("inputs")]
    public required Dictionary<string, float[]> Inputs { get; set; }
}