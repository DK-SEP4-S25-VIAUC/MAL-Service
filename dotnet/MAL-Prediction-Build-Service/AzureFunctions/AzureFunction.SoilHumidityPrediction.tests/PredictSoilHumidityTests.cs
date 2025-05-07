using System.Net;
using System.Text;
using System.Text.Json;
using AzureFunction.SoilHumidityPrediction.tests.HelperClasses;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq; 
using Sep4.PredictionApp;
using Sep4.PredictionApp.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace AzureFunction.SoilHumidityPrediction.tests;

public class PredictSoilHumidityTests : IDisposable
{
    private readonly Mock<ILogger<PredictSoilHumidity>> _loggerMock = new();
    private Mock<IBlobDownloader>? _blobDownloaderMock;
    private Mock<IInferenceSession>? _inferenceSessionMock;
    private Mock<IEnvironmentService>? _envServiceMock;
    private Mock<IModelSessionFactory>? _sessionFactoryMock;
    private TestHttpResponseData? _mockHttpResponseData;
    private TestHttpRequestData? _mockHttpRequestData;
    private PredictSoilHumidity? _function;
    private string? _requestBody;
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public PredictSoilHumidityTests(ITestOutputHelper output) {
        _output = output;
        _requestBody = "";
        _blobDownloaderMock = new();
        _inferenceSessionMock = new ();
        _envServiceMock = new ();
        _sessionFactoryMock = new();
        _function = new PredictSoilHumidity(_loggerMock.Object, _envServiceMock.Object, _blobDownloaderMock.Object, _sessionFactoryMock.Object);
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() {
        _mockHttpResponseData = null;
        _mockHttpRequestData = null;
        _requestBody = null;
        _blobDownloaderMock = null;
        _inferenceSessionMock = null;
        _envServiceMock = null;
        _sessionFactoryMock = null;
        _function = null;
    }
    
    
    [Fact]
    public async Task Run_ThrowsArgumentNullException_WhenHttpRequestIsNull() {
        // Arrange:
        HttpRequestData? request = null;

        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => _function.Run(request!));
    }

    
    [Fact]
    public async Task Run_ReturnsOkWithPredictionValue_WhenHttpRequestIsValid() {
       
        // Arrange:
        var context = new Mock<FunctionContext>().Object;
        _mockHttpResponseData = new TestHttpResponseData(context);

        // Mock IEnvironmentService:
        string testUri = "https://mockUri/dummy_model.onnx";
        string tempModelPath = Path.Combine(Path.GetTempPath(), "dummy_model.onnx");
        _envServiceMock?.Setup(s => s.GetEnvironmentVariable("OnnxModelUri")).Returns(testUri);
        
        // Mock IBlobDownloader:
        _blobDownloaderMock?.Setup(b => b.DownloadAsync(testUri, tempModelPath)).Returns(Task.CompletedTask);

        // Mock InferenceSession and prediction results:
        var predictionTensor = new DenseTensor<float>(new[] { 42f }, new[] { 1 });
        var predictionValue = NamedOnnxValue.CreateFromTensor("output", predictionTensor);
        var mockResults = new MockDisposableList<NamedOnnxValue> { predictionValue };
        var wrappedResults = new NamedOnnxValueWrapper(mockResults);

        _sessionFactoryMock!.Setup(f => f.Create(tempModelPath))
            .Returns(_inferenceSessionMock!.Object);
        
        _inferenceSessionMock!
            .Setup(s => s.Run(It.IsAny<IReadOnlyCollection<NamedOnnxValue>>()))
            .Returns(wrappedResults);
        
        // Mock input
        var input = new PredictionInput {
            Inputs = new Dictionary<string, float[]> {
                ["soil_humidity"] = new [] { 10.0f },
                ["soil_delta"] = new [] { 0.1f },
                ["air_humidity"] = new [] { 50.0f },
                ["temperature"] = new [] { 22.0f },
                ["light"] = new [] { 1000.0f },
                ["hour_sin"] = new [] { 0.5f },
                ["hour_cos"] = new [] { 0.5f }
            }
        };
        
        var requestBody = JsonSerializer.Serialize(input);
        _output.WriteLine("requestBody is: " + requestBody);
        var ms = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));

        // Mock HttpRequestData:
        _mockHttpRequestData = new TestHttpRequestData(context, ms, _mockHttpResponseData);

        _loggerMock.Object.LogInformation("Dummy log call");
        // Act:
        var response = await _function.Run(_mockHttpRequestData);

        
        // Assert:
        Assert.NotNull(response);
        _output.WriteLine("ResponseBody is: " + response.Body);
        response.Body.Position = 0;
        var responseBody1 = await new StreamReader(response.Body).ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody1}");
        _output.WriteLine("ResponseStatusCode is: " + response.StatusCode);
        _output.WriteLine("ResponseContext is: " + response.FunctionContext);
        _output.WriteLine("ResponseHeader is: " + response.Headers);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        response.Body.Position = 0;
        var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
        var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
        Assert.NotNull(responseJson);
        Assert.Equal("Prediction describes how many minutes are estimated before soil humidity falls below a 20% threshold", responseJson["description"].ToString());
        Assert.Equal(42.0, Convert.ToDouble(responseJson["minutes_to_dry"]));
    }
}