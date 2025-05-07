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
///     "hour_cos": [45.7]
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
    /// <summary>
    /// The logger instance used to log information, warnings, and errors during the function's execution.
    /// </summary>
    private readonly ILogger<PredictSoilHumidity> _logger;
    
    private readonly IEnvironmentService _envService;
    private readonly IBlobDownloader _blobDownloader;
    private readonly IModelSessionFactory _sessionFactory;
    
    /// <summary>
    /// A cached instance of the ONNX model's inference session, shared across function invocations to improve performance.
    /// </summary>
    private static IInferenceSession? _cachedSession;
    
    /// <summary>
    /// The URI of the ONNX model in Azure Blob Storage, cached to detect changes in the model URI.
    /// </summary>
    private static string? _cachedUri;

    // Required features for this model / prediction type:
    private const string FeatureNameSoilHumidity = "soil_humidity";
    private const string FeatureNameSoilDelta = "soil_delta";
    private const string FeatureNameAirHumidity = "air_humidity";
    private const string FeatureNameTemperature = "temperature";
    private const string FeatureNameLight = "light";
    private const string FeatureNameHourSin = "hour_sin";
    private const string FeatureNameHourCos = "hour_cos";

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictSoilHumidity"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for logging function execution details.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <param name="logger"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// <param name="envService"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// <param name="blobDownloader"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// <param name="modelSessionFactory"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// </remarks>
    public PredictSoilHumidity(ILogger<PredictSoilHumidity> logger, IEnvironmentService envService, IBlobDownloader blobDownloader, IModelSessionFactory modelSessionFactory) {
        _logger = logger;
        _envService = envService;
        _blobDownloader = blobDownloader;
        _sessionFactory = modelSessionFactory;
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
    // TODO: Test this:
    // 1. Exception if HttpRequestData is null
    // 2. BadRequest if HttpRequestData does not contain ALL required parameters.
    // 3. Error if the .onnx model could not be found (or is not proper format)
    // 4. Error if the float arrays for parameters do nat take exactly 1 value!
    // 5. Error if the float arrays are not float arrays (i.e. strings, or something else)
    [Function("PredictSoilHumidity")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req) {
        _logger.LogInformation("PredictSoilHumidity function triggered.");
        
        if (req == null) {
            _logger.LogError("PredictSoilHumidity function shutdown prematurely. HttpRequestData cannot be 'null'. ");
            throw new ArgumentNullException(nameof(req));
        }
        
        try {
            // Load ONNX model:
            var session = await GetOrLoadModelAsync();
            
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
                errorMsg = $"Invalid input: Could not find 'inputs.{FeatureNameSoilHumidity}' in received request body.";
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

            if (!validationPassed) {
                _logger.LogError("{err}", errorMsg);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync(errorMsg);
                return errorResponse;
            }
            
            
            // Extract the feature data/values:
            var valueSoilHumidity = inputData!.Inputs[FeatureNameSoilHumidity];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameSoilHumidity, JsonSerializer.Serialize(valueSoilHumidity));
            
            var valueSoilDelta = inputData.Inputs[FeatureNameSoilDelta];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameSoilDelta, JsonSerializer.Serialize(valueSoilDelta));
            
            var valueAirHumidity = inputData.Inputs[FeatureNameAirHumidity];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameAirHumidity, JsonSerializer.Serialize(valueAirHumidity));
            
            var valueTemperature = inputData.Inputs[FeatureNameTemperature];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameTemperature, JsonSerializer.Serialize(valueTemperature));
            
            var valueLight = inputData.Inputs[FeatureNameLight];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameLight, JsonSerializer.Serialize(valueLight));
            
            var valueHourSin = inputData.Inputs[FeatureNameHourSin];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameHourSin, JsonSerializer.Serialize(valueHourSin));
            
            var valueHourCos = inputData.Inputs[FeatureNameHourCos];
            _logger.LogInformation("Serializing {input}: {feature}", FeatureNameHourCos, JsonSerializer.Serialize(valueHourCos));
            
            
            // Validate the input length for each feature (model expects exactly 1 value):
            bool featureValidationPassed = true;
            
            if (valueSoilHumidity.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameSoilHumidity}' must contain exactly 1 value, got {valueSoilHumidity.Length} values.";
                featureValidationPassed = false;
            }
            
            if (valueSoilDelta.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameSoilDelta}' must contain exactly 1 value, got {valueSoilDelta.Length} values.";
                featureValidationPassed = false;
            }
            
            if (valueAirHumidity.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameAirHumidity}' must contain exactly 1 value, got {valueAirHumidity.Length} values.";
                featureValidationPassed = false;
            }
            
            if (valueTemperature.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameTemperature}' must contain exactly 1 value, got {valueTemperature.Length} values.";
                featureValidationPassed = false;
            }
            
            if (valueLight.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameLight}' must contain exactly 1 value, got {valueLight.Length} values.";
                featureValidationPassed = false;
            }
            
            if (valueHourSin.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameHourSin}' must contain exactly 1 value, got {valueHourSin.Length} values.";
                featureValidationPassed = false;
            }
            
            if (valueHourCos.Length != 1) {
                errorMsg = $"Invalid input: '{FeatureNameHourCos}' must contain exactly 1 value, got {valueHourCos.Length} values.";
                featureValidationPassed = false;
            }

            if (!featureValidationPassed) {
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
                valueHourCos[0]
            };
            
            // Create a 2D tensor for a single sample with 7 features [1, 7]:
            var inputTensor = new DenseTensor<float>(featureVector, new[] { 1, 7 });
            _logger.LogInformation("Input tensor shape: [{Shape}]", string.Join(", ", inputTensor.Dimensions.ToArray()));
            
            
            // Prepare the input for the ONNX model
            var inputs = new List<NamedOnnxValue> {
                NamedOnnxValue.CreateFromTensor("input", inputTensor)
            };

            
            // Run inference:
            using var results = session.Run(inputs);
            var predictionValue = results.First().AsTensor<float>().ToArray()[0]; // Output is float32[1]
            _logger.LogInformation("Prediction result: {Prediction}", predictionValue);

            
            // Build json response:
            var resultJson = JsonSerializer.Serialize(new
            {
                description = "Prediction describes how many minutes are estimated before soil humidity falls below a 20% threshold",
                minutes_to_dry = predictionValue
            });
            
            
            // Return prediction:
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(resultJson);
            _logger.LogInformation("Returned this response: {response}", resultJson);
            return response;
            
        } catch (Exception ex) {
            _logger.LogError(ex, "Error processing prediction.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}, Cause {ex.StackTrace}");
            return errorResponse;
        }
    }
    
    
    /// <summary>
    /// Loads the ONNX model from Azure Blob Storage or returns the cached instance if already loaded.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="InferenceSession"/>
    /// object representing the loaded ONNX model.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <c>OnnxModelUri</c> environment variable is not set.
    /// </exception>
    /// <exception cref="Azure.RequestFailedException">
    /// Thrown if the ONNX model cannot be downloaded from Azure Blob Storage.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if there is an error writing the model file to the temporary path.
    /// </exception>
    /// <remarks>
    /// The ONNX model is cached locally in the Function App to avoid reloading it for each function invocation.
    /// The model is reloaded if the <c>OnnxModelUri</c> environment variable changes.
    /// </remarks>
    private async Task<IInferenceSession> GetOrLoadModelAsync() {
        string? onnxUri = _envService.GetEnvironmentVariable("OnnxModelUri");
        if (string.IsNullOrEmpty(onnxUri)) {
            _logger.LogCritical("OnnxModelUri is not set in configuration. Set it in Azure Function configuration (environment variables).");
            throw new InvalidOperationException("OnnxModelUri is not set in configuration.");
        }

        // Download the specified model to a temp directory:
        if (_cachedSession == null || _cachedUri != onnxUri) {
            _logger.LogInformation("Loading ONNX model...");

            // Downloads to a temporary directory, with the name 'model.onnx'.
            string tempFilePath = Path.Combine(Path.GetTempPath(), "model.onnx");
            await _blobDownloader.DownloadAsync(onnxUri, tempFilePath);

            _cachedSession = _sessionFactory.Create(tempFilePath);
            // TODO: An error is happening with creating the session during testing...
            
            _cachedUri = onnxUri;

            _logger.LogInformation("ONNX model loaded and cached.");
        }
        return _cachedSession;
    }
}