using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Net;
using System.Text.Json;
using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp;

/// <summary>
/// An Azure Function that predicts soil humidity using an ONNX machine learning model.
/// This function accepts HTTP POST requests with a JSON body containing the target soil humidity value,
/// processes the input using the ONNX model, and returns the predicted hours until a certain condition is met.
/// </summary>
/// <remarks>
/// The function expects the ONNX model to be stored in Azure Blob Storage, with the URI specified in the
/// <c>OnnxModelUri</c> environment variable. The model must have an input named "target" (type <c>float32[1]</c>)
/// and an output named "minutes" (type <c>int32[1]</c>).
/// </remarks>
/// <example>
/// An example HTTP POST request to the function:
/// <code>
/// POST https://sep4predictionapp.azurewebsites.net/api/PredictSoilHumidity?code=your-function-key
/// Content-Type: application/json
/// 
/// {
///   "inputs": {
///     "soil_humidity": [45.7],
///     "soil_delta": [45.7],
///     "air_humidity": [45.7],
///     "temperature": [45.7],
///     "light": [45.7],
///     "hour_sin": [45.7],
///     "hour_cos": [45.7],
///     "threshold": [20.0]
///   }
/// }
/// </code>
/// Expected response:
/// <code>
/// {
///     "description": "Prediction describes how many minutes are estimated before soil humidity falls below a 20% threshold"
///     "minutes_to_dry": 12
/// }
/// </code>
/// </example>
public class PredictSoilHumidity
{
    private readonly ILogger<PredictSoilHumidity> _logger;
    private readonly IModelLoader _modelLoader;
    
    // Required features for this model / prediction type:
    private const string FeatureNameSoilHumidity = "soil_humidity";
    private const string FeatureNameSoilDelta = "soil_delta";
    private const string FeatureNameAirHumidity = "air_humidity";
    private const string FeatureNameTemperature = "temperature";
    private const string FeatureNameLight = "light";
    private const string FeatureNameHourSin = "hour_sin";
    private const string FeatureNameHourCos = "hour_cos";
    private const string FeatureNameThreshold = "threshold";

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictSoilHumidity"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for logging function execution details.</param>
    /// <param name="modelLoader">The IModelLoader instance used for loading the onnx model to use for predictions.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <c>null</c>.</exception>
    public PredictSoilHumidity(ILogger<PredictSoilHumidity> logger, IModelLoader modelLoader) {
        _logger = logger;
        _modelLoader = modelLoader;
    }

    
    /// <summary>
    /// Handles HTTP POST requests to predict soil humidity using the ONNX model.
    /// </summary>
    /// <param name="req">The HTTP request data containing the input for prediction in the request body.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="HttpResponseData"/> object
    /// containing the prediction result (the number of hours) in JSON format.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <c>OnnxModelUri</c> environment variable is not set.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown if the request body cannot be deserialized into a <see cref="PredictionInput"/> object.
    /// </exception>
    [Function("PredictSoilHumidity")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req) {
        _logger.LogInformation("PredictSoilHumidity function triggered.");
        
        if (req == null) {
            _logger.LogError("PredictSoilHumidity function shutdown prematurely. HttpRequestData cannot be 'null'. ");
            throw new ArgumentNullException(nameof(req));
        }

        try {
            // Load ONNX model:
            var session = await _modelLoader.GetOrLoadModelAsync();

            if (session == null) {
                throw new InvalidOperationException("Failed to create InferenceSession from model.");
            }


            // Parse input:
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("Request body: {RequestBody}", requestBody);

            var inputData = JsonSerializer.Deserialize<PredictionInput>(requestBody);


            // Validate input:
            bool validationPassed = true;
            string errorMsg = "";
            if (inputData == null) {
                errorMsg = "Invalid input: Request body is null.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameSoilHumidity)) {
                errorMsg =
                    $"Invalid input: Could not find 'inputs.{FeatureNameSoilHumidity}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameSoilDelta)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameSoilDelta}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameAirHumidity)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameAirHumidity}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameTemperature)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameTemperature}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameLight)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameLight}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameHourSin)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameHourSin}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameHourCos)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameHourCos}' in received request body.";
                validationPassed = false;
            }

            if (validationPassed && !inputData!.Inputs.ContainsKey(FeatureNameThreshold)) {
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameThreshold}' in received request body.";
                validationPassed = false;
            }

            if (!validationPassed) {
                _logger.LogError("{err}", errorMsg);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync(errorMsg);
                return errorResponse;
            }


            // Extract the feature data/values:
            var valueSoilHumidity = inputData!.Inputs[FeatureNameSoilHumidity];
            var valueSoilDelta = inputData.Inputs[FeatureNameSoilDelta];
            var valueAirHumidity = inputData.Inputs[FeatureNameAirHumidity];
            var valueTemperature = inputData.Inputs[FeatureNameTemperature];
            var valueLight = inputData.Inputs[FeatureNameLight];
            var valueHourSin = inputData.Inputs[FeatureNameHourSin];
            var valueHourCos = inputData.Inputs[FeatureNameHourCos];
            var valueThreshold = inputData.Inputs[FeatureNameThreshold];

            try {
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameSoilHumidity,
                    JsonSerializer.Serialize(valueSoilHumidity));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameSoilDelta,
                    JsonSerializer.Serialize(valueSoilDelta));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameAirHumidity,
                    JsonSerializer.Serialize(valueAirHumidity));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameTemperature,
                    JsonSerializer.Serialize(valueTemperature));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameLight,
                    JsonSerializer.Serialize(valueLight));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameHourSin,
                    JsonSerializer.Serialize(valueHourSin));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameHourCos,
                    JsonSerializer.Serialize(valueHourCos));
                _logger.LogInformation("Serializing {input}: {feature}", FeatureNameThreshold,
                    JsonSerializer.Serialize(valueThreshold));
            } catch (Exception ex) {
                _logger.LogError("Serializing input failed.");
                throw new JsonException(
                    $"Failed to serialize received input as json. Please verify that your http request contains data properly formatted in json and compatible with this prediction model.\nCause: {ex.Message}");
            }



            // Validate the input length for each feature (model expects exactly 1 value):
            bool featureValidationPassed = true;

            if (valueSoilHumidity.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameSoilHumidity}' must contain exactly 1 value, got {valueSoilHumidity.Length} values.";
                featureValidationPassed = false;
            }

            if (valueSoilDelta.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameSoilDelta}' must contain exactly 1 value, got {valueSoilDelta.Length} values.";
                featureValidationPassed = false;
            }

            if (valueAirHumidity.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameAirHumidity}' must contain exactly 1 value, got {valueAirHumidity.Length} values.";
                featureValidationPassed = false;
            }

            if (valueTemperature.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameTemperature}' must contain exactly 1 value, got {valueTemperature.Length} values.";
                featureValidationPassed = false;
            }

            if (valueLight.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameLight}' must contain exactly 1 value, got {valueLight.Length} values.";
                featureValidationPassed = false;
            }

            if (valueHourSin.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameHourSin}' must contain exactly 1 value, got {valueHourSin.Length} values.";
                featureValidationPassed = false;
            }

            if (valueHourCos.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameHourCos}' must contain exactly 1 value, got {valueHourCos.Length} values.";
                featureValidationPassed = false;
            }

            if (valueThreshold.Length != 1) {
                errorMsg =
                    $"Invalid input: '{FeatureNameThreshold}' must contain exactly 1 value, got {valueThreshold.Length} values.";
                featureValidationPassed = false;
            }

            if (!featureValidationPassed) {
                _logger.LogError("{err}", errorMsg);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync(errorMsg);
                return errorResponse;
            }

            // Validate the values in each received feature:
            bool valueValidationPassed = true;
            // Check if sensor data values are either below 0 or above 1023
            if (valueSoilHumidity[0] < 0 || valueSoilHumidity[0] > 1023) {
                errorMsg =
                    $"Invalid input. 'soil_humidity' must be between 0 and 1023. Current value is: '{valueSoilHumidity[0]}'";
                valueValidationPassed = false;
            }

            if (valueAirHumidity[0] < 0 || valueAirHumidity[0] > 100) {
                errorMsg =
                    $"Invalid input. 'air_humidity' must be between 0% and 100%. Current value is: '{valueAirHumidity[0]}'";
                valueValidationPassed = false;
            }

            if (valueTemperature[0] < -20 || valueTemperature[0] > 100) {
                errorMsg =
                    $"Invalid input. 'temperature' must be between -20 and 100 degrees celsius. Current value is: '{valueTemperature[0]}'";
                valueValidationPassed = false;
            }

            if (valueLight[0] < 0 || valueLight[0] > 10000) {
                errorMsg =
                    $"Invalid input. 'light' must be between 0 and 10000. Current value is: '{valueLight[0]}'";
                valueValidationPassed = false;
            }

            if (valueThreshold[0] < 0 || valueThreshold[0] > 100) {
                errorMsg =
                    $"Invalid input. 'threshold' must be between 0 and 100%. Current value is: '{valueThreshold[0]}'";
                valueValidationPassed = false;
            }

            // Check if sin and cos values are between -1 and 1:
            if (valueHourSin[0] < -1 || valueHourSin[0] > 1) {
                errorMsg = $"Invalid input. 'hour_sin' must be between -1 and 1. Current value is: '{valueHourSin[0]}'";
                valueValidationPassed = false;
            }

            if (valueHourCos[0] < -1 || valueHourCos[0] > 1) {
                errorMsg = $"Invalid input. 'hour_cos' must be between -1 and 1. Current value is: '{valueHourCos[0]}'";
                valueValidationPassed = false;
            }

            if (!valueValidationPassed) {
                _logger.LogError("{err}", errorMsg);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync(errorMsg);
                return errorResponse;
            }


            // Build a feature vector to use in inference:
            var featureVector = new[] {
                valueSoilHumidity[0],
                valueSoilDelta[0],
                valueAirHumidity[0],
                valueTemperature[0],
                valueLight[0],
                valueHourSin[0],
                valueHourCos[0],
                valueThreshold[0]
            };

            // Create a 2D tensor for a single sample with 8 features [1, 8]:
            var inputTensor = new DenseTensor<float>(featureVector, new[] { 1, 8 });
            _logger.LogInformation("Input tensor shape: [{Shape}]",
                string.Join(", ", inputTensor.Dimensions.ToArray()));


            // Prepare the input for the ONNX model
            var inputs = new List<NamedOnnxValue> {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };


            // Run inference:
            using var results = session.Run(inputs);
            var predictionValue = results.First().AsTensor<float>().ToArray()[0]; // Output is float32[1]
            _logger.LogInformation("Prediction result: {Prediction}", predictionValue);


            // Build json response:
            var resultJson = JsonSerializer.Serialize(new {
                description =
                    $"Prediction of the estimated minutes until soil humidity falls below a {valueThreshold[0]}% threshold",
                minutes_to_dry = predictionValue
            });


            // Return prediction:
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(resultJson);
            _logger.LogInformation("Returned this response: {response}", resultJson);
            return response;

        } catch (InvalidOperationException ex) {
            _logger.LogError("{err}", ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.Gone);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;

        } catch (ArgumentException ex) {
            _logger.LogError("{err}", ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;

        } catch (FileNotFoundException ex) {
            _logger.LogError("{err}", ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;

        } catch (JsonException ex) {
            _logger.LogError("{err}", ex.Message);
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync(ex.Message);
            return errorResponse;
            
        } catch (Exception ex) {
            _logger.LogError(ex, "Error processing prediction.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message},\nCause {ex.StackTrace}");
            return errorResponse;
        }
    }
}