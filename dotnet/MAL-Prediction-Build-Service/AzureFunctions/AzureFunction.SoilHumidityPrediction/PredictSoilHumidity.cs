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

public class PredictSoilHumidity
{
    private readonly ILogger<PredictSoilHumidity> _logger;
    private static InferenceSession? _cachedSession;
    private static string? _cachedUri;

    public PredictSoilHumidity(ILogger<PredictSoilHumidity> logger) {
        _logger = logger;
    }

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
            
            // Extract the target input
            var target = inputData.Inputs["target"];
            _logger.LogInformation("Target input: {Target}", JsonSerializer.Serialize(target));
            
            // Validate the input length (model expects exactly 1 value)
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