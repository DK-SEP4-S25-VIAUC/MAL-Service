using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Azure.Identity;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Net;
using System.Text.Json;

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
///     "target": [45.7]
///   }
/// }
/// </code>
/// Expected response:
/// <code>
/// {
///   "minutes": 12
/// }
/// </code>
/// </example>
public class PredictSoilHumidity
{
    /// <summary>
    /// The logger instance used to log information, warnings, and errors during the function's execution.
    /// </summary>
    private readonly ILogger<PredictSoilHumidity> _logger;
    
    /// <summary>
    /// A cached instance of the ONNX model's inference session, shared across function invocations to improve performance.
    /// </summary>
    private static InferenceSession? _cachedSession;
    
    /// <summary>
    /// The URI of the ONNX model in Azure Blob Storage, cached to detect changes in the model URI.
    /// </summary>
    private static string? _cachedUri;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictSoilHumidity"/> class.
    /// </summary>
    /// <param name="logger">The logger instance used for logging function execution details.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <c>null</c>.</exception>
    /// <remarks>
    /// <param name="logger"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// </remarks>
    public PredictSoilHumidity(ILogger<PredictSoilHumidity> logger) {
        _logger = logger;
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
    /// <remarks>
    /// The request body must contain a JSON object with an "inputs" field, which includes a "target" key
    /// with a float array value containing exactly one element (e.g., [45.7]). The function validates the input,
    /// runs inference using the ONNX model, and returns the predicted minutes as an integer.
    /// </remarks>
    [Function("PredictSoilHumidity")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req) {
        _logger.LogInformation("PredictSoilHumidity function triggered.");
        
        try {
            // Load ONNX model:
            var session = await GetOrLoadModelAsync();

            // Parse input:
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("Request body: {RequestBody}", requestBody);
            
            var inputData = JsonSerializer.Deserialize<PredictionInput>(requestBody);
            
            if (inputData == null || !inputData.Inputs.ContainsKey("target")) {
                _logger.LogError("Invalid input: 'inputs.target' is missing or null.");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid input: 'inputs.target' is required.");
                return errorResponse;
            }
            
            // Extract the target input:
            var target = inputData.Inputs["target"];
            _logger.LogInformation("Target input: {Target}", JsonSerializer.Serialize(target));
            
            // Validate the input length (model expects exactly 1 value):
            if (target.Length != 1) {
                _logger.LogError("Invalid input: 'target' must contain exactly 1 value, got {Length}.", target.Length);
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Invalid input: 'target' must contain exactly 1 value.");
                return errorResponse;
            }
            
            // Create a 1D tensor:
            var inputTensor = new DenseTensor<float>(new[] { target[0] }, [1]);
            _logger.LogInformation("Input tensor shape: [{Shape}]", string.Join(", ", inputTensor.Dimensions.ToArray()));
            
            // Prepare the input for the ONNX model
            var inputs = new List<NamedOnnxValue> {
                NamedOnnxValue.CreateFromTensor("target", inputTensor)
            };

            // Run inference:
            using var results = session.Run(inputs);
            var prediction = results.First().AsTensor<int>().ToArray(); // Output is int32[1]
            _logger.LogInformation("Prediction result: {Prediction}", prediction[0]);

            // Return prediction:
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Prediction: {string.Join(", ", prediction)}");
            _logger.LogInformation("Returned this response: {response}", response);
            return response;
            
        } catch (Exception ex) {
            _logger.LogError(ex, "Error processing prediction.");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
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
    /// The ONNX model is cached in memory to avoid reloading it for each function invocation.
    /// The model is reloaded if the <c>OnnxModelUri</c> environment variable changes.
    /// </remarks>
    private async Task<InferenceSession> GetOrLoadModelAsync() {
        string? onnxUri = Environment.GetEnvironmentVariable("OnnxModelUri");
        if (string.IsNullOrEmpty(onnxUri)) {
            _logger.LogError("OnnxModelUri is not set in configuration.");
            throw new InvalidOperationException("OnnxModelUri is not set in configuration.");
        }

        if (_cachedSession == null || _cachedUri != onnxUri) {
            _logger.LogInformation("Loading ONNX model...");

            string tempFilePath = Path.Combine(Path.GetTempPath(), "model.onnx");
            var blobClient = new BlobClient(new Uri(onnxUri), new DefaultAzureCredential());
            await blobClient.DownloadToAsync(tempFilePath);

            _cachedSession = new InferenceSession(tempFilePath);
            _cachedUri = onnxUri;

            _logger.LogInformation("ONNX model loaded and cached.");
        }
        return _cachedSession;
    }

}