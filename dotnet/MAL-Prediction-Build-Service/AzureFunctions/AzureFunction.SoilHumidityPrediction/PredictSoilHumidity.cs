using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Net;

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

            // Parse input (replace with your actual input model):
            var inputData = new DenseTensor<float>(new[] { 1.0f, 2.0f }, new[] { 1, 2 });
            var inputName = session.InputMetadata.Keys.First();
            var inputs = new[] { NamedOnnxValue.CreateFromTensor(inputName, inputData) };

            // Run inference:
            using var results = session.Run(inputs);
            var prediction = results.First().AsTensor<float>().ToArray();

            // Return prediction:
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Prediction: {string.Join(", ", prediction)}");
            _logger.LogInformation("Returned this response: {response}", response);
            return response;
        }
        catch (Exception ex) {
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
            var blobClient = new BlobClient(new Uri(onnxUri));
            await blobClient.DownloadToAsync(tempFilePath);

            _cachedSession = new InferenceSession(tempFilePath);
            _cachedUri = onnxUri;

            _logger.LogInformation("ONNX model loaded and cached.");
        }

        return _cachedSession;
    }

}