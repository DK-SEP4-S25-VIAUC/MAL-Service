using System.Net;
using System.Reflection;
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
    private Mock<IModelLoader>? _modelLoaderMock;
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
        _modelLoaderMock = new();
        _function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
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
        _modelLoaderMock = null;
        
        // Reset static fields to avoid test interference:
        typeof(PredictSoilHumidity).GetField("_cachedSession", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
        typeof(PredictSoilHumidity).GetField("_cachedUri", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);
    }
    
    
    [Fact]
    public async Task Run_ThrowsArgumentNullException_WhenHttpRequestIsNull() {
        // Arrange:
        HttpRequestData? request = null;

        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => _function.Run(request!));
    }
    
    
    [Fact]
    public async Task Run_ReturnsOkWithPrediction_WhenHttpRequestIsValid() {
        // Arrange:
        // Note: These steps below took my quite a few hours to nail.
        // Mocking Microsoft.ML.OnnxRuntime is a 'pain in the ass' to say it mildly.
        // ChatGPT and GROK3 where overall quite useless, but could help with small details of the code.
        
        // Build a one‐element tensor for our fake prediction:
        float expectedPrediction = 123.45f;
        var tensor = new DenseTensor<float>(new[] { expectedPrediction }, new[] { 1 });

        // Reflectively find the INTERNAL IOrtValueOwner type:
        var ortOwnerType = typeof(DisposableNamedOnnxValue)
            .Assembly
            .GetType("Microsoft.ML.OnnxRuntime.IOrtValueOwner", throwOnError: true);

        // Find the non‐public ctor on DisposableNamedOnnxValue(string, object, TensorElementType, IOrtValueOwner):
        var ctor = typeof(DisposableNamedOnnxValue).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            types: new[] {
                typeof(string),
                typeof(object),
                typeof(TensorElementType),
                ortOwnerType
            },
            modifiers: null
        ) ?? throw new InvalidOperationException("Could not find the expected private constructor.");

        // Invoke that ctor, passing `null` for IOrtValueOwner.
        // This is because IOrtValueOwner cannot be mocked, since its only internal to the OnnxRuntime library.
        var disposableValue = (DisposableNamedOnnxValue)ctor.Invoke(new object[] {
            "output",           // name
            tensor,             // the DenseTensor<float>
            TensorElementType.Float,
            null                // owner—null is fine here
        });

        // Mock the collection that InferenceSession.Run returns
        var mockResults = new Mock<IDisposableReadOnlyCollection<DisposableNamedOnnxValue>>();
        mockResults
            .Setup(r => r.GetEnumerator())
            .Returns(() => new List<DisposableNamedOnnxValue> { disposableValue }
                .GetEnumerator());
        mockResults.Setup(r => r.Dispose());

        // Mock IInferenceSession to return our fake results
        var mockSession = new Mock<IInferenceSession>();
        mockSession
            .Setup(s => s.Run(It.IsAny<IReadOnlyCollection<NamedOnnxValue>>()))
            .Returns(mockResults.Object);

        // Wire up the IModelLoader
        _modelLoaderMock
            .Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(mockSession.Object);

        // Instantiate your Function under test
        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);

        // Build a valid HTTP request
        var inputPayload = new {
            inputs = new Dictionary<string, float[]> {
                ["soil_humidity"] = new[] { 0.1f },
                ["soil_delta"]    = new[] { 0.2f },
                ["air_humidity"]  = new[] { 0.3f },
                ["temperature"]   = new[] { 0.4f },
                ["light"]         = new[] { 0.5f },
                ["hour_sin"]      = new[] { 0.6f },
                ["hour_cos"]      = new[] { 0.7f },
                ["threshold"]     = new[] { 0.8f }
            }
        };
        
        var jsonBody = JsonSerializer.Serialize(inputPayload);
        var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
        var requestStream = new MemoryStream(bodyBytes);

        var functionContext = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(functionContext, requestStream);
        request.Headers.Add("Content-Type", "application/json");

        // Act
        var response = await function.Run(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Headers
            .GetValues("Content-Type").First());

        response.Body.Position = 0;
        using var reader = new StreamReader(response.Body);
        var json = JsonSerializer.Deserialize<JsonElement>(await reader.ReadToEndAsync());
        float actual = json.GetProperty("minutes_to_dry").GetSingle();
        _output.WriteLine($"Response Body: {json}");
        Assert.Contains("description", json.ToString());
        Assert.Contains("minutes_to_dry", json.ToString());
        Assert.Equal(expectedPrediction, actual);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredSoilHumidityParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                // Missing 'soil_humidity'
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 5.0f } },
                { "hour_cos", new[] { 6.0f } },
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("soil_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredSoilDeltaParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                // Missing 'soil_delta'
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 5.0f } },
                { "hour_cos", new[] { 6.0f } },
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("soil_delta", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredAirHumidityParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                // Missing 'air_humidity'
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 5.0f } },
                { "hour_cos", new[] { 6.0f } },
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("air_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredTemperatureParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                // Missing 'temperature'
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 5.0f } },
                { "hour_cos", new[] { 6.0f } },
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("temperature", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredLightParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                // Missing 'light'
                { "hour_sin", new[] { 5.0f } },
                { "hour_cos", new[] { 6.0f } },
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("light", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredHourSinParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                // Missing 'hour_sin'
                { "hour_cos", new[] { 6.0f } },
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_sin", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredHourCosParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 5.0f } },
                // Missing 'hour_cos'
                { "threshold", new[] { 7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_cos", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestIsMissingRequiredThresholdParameter() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 5.0f } },
                { "hour_cos", new[] { 5.0f } },
                // Missing 'threshold'
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("threshold", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestThresholdParameterIsBelow0() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { -7.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("threshold", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestThresholdParameterIsAbove100() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.1f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("threshold", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestHourCosParameterIsBelowNeg1() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { -1.01f } },
                { "threshold", new[] { 0.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_cos", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestHourCosParameterIsAbove1() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { 1.01f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_cos", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestHourSinParameterIsBelowNeg1() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { -1.01f } },
                { "hour_cos", new[] { -1.0f } },
                { "threshold", new[] { 0.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_sin", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestHourSinParameterIsAbove1() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 4.0f } },
                { "hour_sin", new[] { 1.01f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_sin", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestLightParameterIsBelow0() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { -0.01f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { -1.0f } },
                { "threshold", new[] { 0.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("light", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestLightParameterIsAbove10000() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 3.0f } },
                { "light", new[] { 10000.01f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("light", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestTemperatureParameterIsBelowNeg20() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { -20.01f } },
                { "light", new[] { 0.01f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { -1.0f } },
                { "threshold", new[] { 0.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("temperature", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestTemperatureParameterIsAbove100() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 2.0f } },
                { "temperature", new[] { 100.01f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("temperature", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestAirHumidityParameterIsBelow0() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { -0.01f } },
                { "temperature", new[] { 0.01f } },
                { "light", new[] { 0.01f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { -1.0f } },
                { "threshold", new[] { 0.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("air_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestAirHumidityParameterIsAbove100() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 0.1f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.01f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("air_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestSoilHumidityParameterIsBelow0() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { -0.01f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 0.01f } },
                { "temperature", new[] { 0.01f } },
                { "light", new[] { 0.01f } },
                { "hour_sin", new[] { -1.0f } },
                { "hour_cos", new[] { -1.0f } },
                { "threshold", new[] { 0.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("soil_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestSoilHumidityParameterIsAbove1023() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 1023.01f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("soil_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestDataIsNotFormattedAsJson() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);

        string input = "this is not a JSON format";

        string jsonString = JsonSerializer.Serialize(input);
        
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestDataIsNotProperlyFormattedAsStringKeysAndFloatValues() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, string> {
                ["soil_humidity"] = "32.0",
                ["soil_delta"] = "5.2",
                ["air_humidity"] = "22.4",
                ["temperature"] = "16.2" ,
                ["light"] = "42.1",
                ["hour_sin"] = "0.72",
                ["hour_cos"] = "0.32",
                ["threshold"] = "12.1"
            }
        };

        string jsonString = JsonSerializer.Serialize(input);
        
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestSoilHumidityParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f , 100.0f }},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("soil_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestSoilDeltaParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f, 0.8f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("soil_delta", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestAirHumidityParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f, 99.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("air_humidity", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestTemperatureParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f, 5.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("temperature", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestLightParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f, 42.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("light", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestHourSinParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f, 0.9f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_sin", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestHourCosParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f, 0.8f } },
                { "threshold", new[] { 100.0f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("hour_cos", responseBody);
    }
    
    
    [Fact]
    public async Task Run_ReturnsBadRequest_WhenHttpRequestThresholdParameterContains2FloatValuesInsteadOfThe1Required() {
        // Arrange:
        _modelLoaderMock.Setup(m => m.GetOrLoadModelAsync())
            .ReturnsAsync(Mock.Of<IInferenceSession>());

        var function = new PredictSoilHumidity(_loggerMock.Object, _modelLoaderMock.Object);
        
        var input = new {
            inputs = new Dictionary<string, float[]> {
                { "soil_humidity", new[] { 100.0f}},
                { "soil_delta", new[] { 1.0f } },
                { "air_humidity", new[] { 100.0f } },
                { "temperature", new[] { 100.0f } },
                { "light", new[] { 100.0f } },
                { "hour_sin", new[] { 1.0f } },
                { "hour_cos", new[] { 1.0f } },
                { "threshold", new[] { 100.0f, 98.2f } }
            }
        };

        string requestBody = JsonSerializer.Serialize(input);
        var bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        
        var context = new Mock<FunctionContext>().Object;
        var request = new TestHttpRequestData(context, bodyStream);

        
        // Act:
        var result = await function.Run(request);

        
        // Assert:
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

        result.Body.Position = 0;
        using var reader = new StreamReader(result.Body);
        string responseBody = await reader.ReadToEndAsync();
        _output.WriteLine($"Response Body: {responseBody}");

        Assert.Contains("threshold", responseBody);
    }
}