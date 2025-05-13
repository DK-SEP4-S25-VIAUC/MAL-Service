using Azure.ResourceManager;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PredictionBuildService.Configurations;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;
using PredictionBuildService.Infrastructure.Build;
using PredictionBuildService.Infrastructure.Evaluation;
using PredictionBuildService.Infrastructure.Monitoring;
using PredictionBuildService.Infrastructure.Tests.HelperClasses;
using Xunit.Abstractions;

namespace PredictionBuildService.Infrastructure.Tests.Build;

public class BuildServiceImplTests : IDisposable
{
    private Mock<ILogger<BuildServiceImpl>>? _mockLoggerBuildService;
    private Mock<ILogger<ModelEvaluationServiceImpl>>? _mockLoggerEvaluationService;
    private Mock<ILogger<BlobStorageMonitorServiceImpl>>? _mockLoggerBlobStorageMonitorService;
    private Mock<IModelCache>? _mockModelCache;
    private AzureFunctionsSettings? _azureFunctionSettings;
    private Mock<ArmClient>? _mockArmClient;
    private IModelEvaluationService? _modelEvaluationService;
    private IBuildService? _buildService;
    private Mock<QueueClient>? _mockQueueClient;
    private Mock<IBlobStorageInteractionHelper>? _mockBlobStorageInteractionHelper;
    private Mock<BlobServiceClient>? _mockBlobServiceClient;
    private IBlobStorageMonitorService _blobStorageMonitorService;
    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public BuildServiceImplTests(ITestOutputHelper output) { 
        _mockModelCache = new ();
        _mockArmClient = new();
        _mockLoggerEvaluationService = new();
        _mockLoggerBlobStorageMonitorService = new();
        _mockQueueClient = new ();
        _mockBlobStorageInteractionHelper = new ();
        _mockBlobServiceClient = new ();
        _mockLoggerBuildService = new();
        
        // Set up General Purpose Mocks:
        Mock<IOptions<AzureBlobStorageSettings>> mockBlobStorageSettings = new();
        string containerName = "models";
        string storageAccountUri = "https://modelregistrymal.blob.core.windows.net/";
        string queueUri = "https://modelregistrymal.queue.core.windows.net/blob-events";
        string modelFileType = ".onnx";
        string modelMetaDataFormat = ".metadata.json";
        mockBlobStorageSettings
            .Setup(o => o.Value)
            .Returns(new AzureBlobStorageSettings {
                ContainerName = containerName,
                StorageAccountUri = storageAccountUri,
                QueueUri = queueUri,
                ModelFileType = modelFileType,
                ModelMetaDataFormat = modelMetaDataFormat
            });
        
        Mock<IOptions<WorkerSettings>> mockWorkerSettings = new();
        string sleepBetweenChecksInterval = "1";
        mockWorkerSettings
            .Setup(o => o.Value)
            .Returns(new WorkerSettings {
                SleepBetweenChecksInterval = sleepBetweenChecksInterval
            });
        
        Mock<IOptions<AzureFunctionsSettings>> mockAzureFunctionSettings = new();
        mockAzureFunctionSettings
            .Setup(o => o.Value)
            .Returns(new AzureFunctionsSettings {
                SubscriptionId = "7221b234-1234-1234-1234-120997325262",
                ResourceGroupName = "rg-group",
                FunctionAppName = "testApp",
                EnvironmentVariableName_OnnxBestSoilPredictionModelUri = "OnnxBestSoilPredictionModelUri"
            });
        _azureFunctionSettings = mockAzureFunctionSettings.Object.Value;
        
        _blobStorageMonitorService = new BlobStorageMonitorServiceImpl(
            _mockLoggerBlobStorageMonitorService.Object, 
            mockBlobStorageSettings.Object,
            _mockQueueClient.Object,
            _mockModelCache.Object,
            _mockBlobStorageInteractionHelper.Object,
            _mockBlobServiceClient.Object,
            mockWorkerSettings.Object);
        
        _modelEvaluationService = new ModelEvaluationServiceImpl(
            _mockLoggerEvaluationService.Object,
            _mockModelCache.Object,
            _blobStorageMonitorService);
        
        _buildService = new BuildServiceImpl(
            _mockLoggerBuildService.Object, 
            _modelEvaluationService,
            mockAzureFunctionSettings.Object,
            _mockArmClient.Object);
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() { 
        _mockModelCache = null;
        _mockArmClient = null;
        _azureFunctionSettings = null;
        _modelEvaluationService = null;
        _buildService = null;
        _mockLoggerEvaluationService = null;
        _mockLoggerBlobStorageMonitorService = null;
        _mockLoggerBuildService = null;
    }
    
    
    [Fact]
    public async Task Subscribe_HandlesEvent_WhenSubscribedToEvaluatedAllSoilHumidityPredictionModelsEventArgsAndThisEventIsFiredInModelEvaluationServiceImpl() {
        // Arrange:
        var badModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-06-05T19:55:53.716723",
            Target = "minutes_to_dry (<20 % soil humidity)",
            FeatureNames = [
                "soil_humidity",
                "soil_delta",
                "air_humidity",
                "temperature",
                "light",
                "hour_sin",
                "hour_cos"
            ],
            DownloadUrl = "https://example.com/test1.onnx",
            Alpha = 124.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        var betterModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-06-05T22:55:53.716723",
            Target = "minutes_to_dry (<20 % soil humidity)",
            FeatureNames = [
                "soil_humidity",
                "soil_delta",
                "air_humidity",
                "temperature",
                "light",
                "hour_sin",
                "hour_cos"
            ],
            DownloadUrl = "https://example.com/test2.onnx",
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
            RmseCv = 43.93,
            R2 = 0.85
        };
        
        var existingModels = new[] { badModel, betterModel };
        _mockModelCache
            .Setup(c => c.ListModelsAsync())
            .Returns(new MockAsyncEnumerable<ModelDTO>(existingModels));


        // Act:
        try {
            await _blobStorageMonitorService.NotifySubscribersAsync();
        } catch (Exception ex) {
            // ignored
        }
        

        // Assert:
        _mockLoggerEvaluationService.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("No subscribers to the AllSoilHumidityModelsEvaluated event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never
        );
        
        _mockLoggerEvaluationService.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("ModelEvaluationService subscribed to events from BlobStorageMonitorService")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
        
        // Check if the log messages written in ModelEvaluationServiceImpl after receiving the event
        // notification from the BlobStorageMonitorServiceImpl
        _mockLoggerBuildService.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("BuildService received EvaluatedAllSoilHumidityPredictionModelsEventArgs event. Building and deploying updated Linear Model to Azure Functions App")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    
    [Fact]
    public async Task Unsubscribe_DoesNotHandleEvent_WhenUnsubscribedToEvaluatedAllSoilHumidityPredictionModelsEventArgsAndThisEventIsFiredInModelEvaluationServiceImpl() {
        // Arrange:
        var badModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-06-05T19:55:53.716723",
            Target = "minutes_to_dry (<20 % soil humidity)",
            FeatureNames = [
                "soil_humidity",
                "soil_delta",
                "air_humidity",
                "temperature",
                "light",
                "hour_sin",
                "hour_cos"
            ],
            DownloadUrl = "https://example.com/test1.onnx",
            Alpha = 124.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        var betterModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-06-05T22:55:53.716723",
            Target = "minutes_to_dry (<20 % soil humidity)",
            FeatureNames = [
                "soil_humidity",
                "soil_delta",
                "air_humidity",
                "temperature",
                "light",
                "hour_sin",
                "hour_cos"
            ],
            DownloadUrl = "https://example.com/test2.onnx",
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
            RmseCv = 43.93,
            R2 = 0.85
        };
        
        var existingModels = new[] { badModel, betterModel };
        _mockModelCache
            .Setup(c => c.ListModelsAsync())
            .Returns(new MockAsyncEnumerable<ModelDTO>(existingModels));


        _buildService.Unsubscribe();
        
        
        // Act:
        try {
            await _blobStorageMonitorService.NotifySubscribersAsync();
        } catch (Exception ex) {
            // ignored
        }
        

        // Assert:
        _mockLoggerEvaluationService.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("ModelEvaluationService subscribed to events from BlobStorageMonitorService")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
        
        // Check if the log messages written in ModelEvaluationServiceImpl after receiving the event
        // notification from the BlobStorageMonitorServiceImpl
        _mockLoggerBuildService.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("BuildService received EvaluatedAllSoilHumidityPredictionModelsEventArgs event. Building and deploying updated Linear Model to Azure Functions App")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never
        );
    }
}