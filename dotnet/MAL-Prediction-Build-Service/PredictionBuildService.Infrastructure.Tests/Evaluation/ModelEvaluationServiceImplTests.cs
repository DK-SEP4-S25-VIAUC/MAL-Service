using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PredictionBuildService.Configurations;
using PredictionBuildService.core.EventArgs;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;
using PredictionBuildService.Infrastructure.Evaluation;
using PredictionBuildService.Infrastructure.Monitoring;
using PredictionBuildService.Infrastructure.Tests.HelperClasses;

namespace PredictionBuildService.Infrastructure.Tests.Evaluation;

/// <summary>
/// Automated Unit Testing for the class with the same name (with the 'Tests' part).
/// </summary>
public class ModelEvaluationServiceImplTests : IDisposable
{
    private readonly Mock<ILogger<ModelEvaluationServiceImpl>> _mockLogger = new ();
    private Mock<IModelCache>? _mockModelCache;
    private Mock<QueueClient>? _mockQueueClient;
    private AzureBlobStorageSettings? _blobStorageSettings;
    private Mock<IBlobStorageInteractionHelper>? _mockBlobStorageInteractionHelper;
    private Mock<BlobServiceClient>? _mockBlobServiceClient;
    private WorkerSettings? _workerSettings;
    private IBlobStorageMonitorService _blobStorageMonitorService;
    private IModelEvaluationService _modelEvaluationService;
    
    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public ModelEvaluationServiceImplTests() {
        _mockModelCache = new ();
        _mockQueueClient = new ();
        _blobStorageSettings = new ();
        _mockBlobStorageInteractionHelper = new ();
        _mockBlobServiceClient = new ();
        _workerSettings = new ();
        
        // Set up General Purpose Mocks:
        Mock<IOptions<AzureBlobStorageSettings>> mockBlobStorageSettings = new();
        string containerName = "models";
        string storageAccountUri = "https://modelregistrymal.blob.core.windows.net/";
        string queueUri = "https://modelregistrymal.queue.core.windows.net/blob-events";
        string modelFileType = ".onnx";
        string modelMetaDataFormat = ".metadata.json";
        mockBlobStorageSettings
            .Setup(o => o.Value)
            .Returns(new AzureBlobStorageSettings
            {
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
        
        Mock<ILogger<BlobStorageMonitorServiceImpl>> mockLoggerBlobMonitor = new();
        
        _blobStorageMonitorService = new BlobStorageMonitorServiceImpl(
            mockLoggerBlobMonitor.Object, 
            mockBlobStorageSettings.Object,
            _mockQueueClient.Object,
            _mockModelCache.Object,
            _mockBlobStorageInteractionHelper.Object,
            _mockBlobServiceClient.Object,
            mockWorkerSettings.Object);
        
        _modelEvaluationService = new ModelEvaluationServiceImpl(
            _mockLogger.Object,
            _mockModelCache.Object,
            _blobStorageMonitorService);
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() {
        _mockModelCache = null;
        _mockQueueClient = null;
        _blobStorageSettings = null;
        _mockBlobStorageInteractionHelper = null;
        _mockBlobServiceClient = null;
        _workerSettings = null;
    }
    
    
    [Fact]
    public async Task Subscribe_HandlesEvent_WhenSubscribedToAddedNewModelsEventArgsAndThisEventIsFiredInBlobStorageMonitorServiceImpl() {
        // Arrange:
        var model1 = new LinearRegressionModelDTO {
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
        
        var model2 = new LinearRegressionModelDTO {
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
            Alpha = 122.29807108324337,
            CrossValSplits = 2,
            RmseCv = 352.93,
            R2 = 0.52
        };
        
        var existingModels = new[] { model1, model2 };
        _mockModelCache
            .Setup(c => c.ListModelsAsync())
            .Returns(new MockAsyncEnumerable<ModelDTO>(existingModels));
        
        // Act:
        await _blobStorageMonitorService.NotifySubscribersAsync();
        
        // Assert:
        // Check if the log messages written in ModelEvaluationServiceImpl after receiving the event
        // notification from the BlobStorageMonitorServiceImpl
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Evaluation found best SoilHumidityPrediction model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    
    [Fact]
    public async Task Unsubscribe_IgnoresEvent_WhenUnsubscribedToAddedNewModelsEventArgsAndThisEventIsFiredInBlobStorageMonitorServiceImpl() {
        // Arrange:
        var model1 = new LinearRegressionModelDTO {
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
        
        var model2 = new LinearRegressionModelDTO {
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
            Alpha = 122.29807108324337,
            CrossValSplits = 2,
            RmseCv = 352.93,
            R2 = 0.52
        };
        
        var existingModels = new[] { model1, model2 };
        _mockModelCache
            .Setup(c => c.ListModelsAsync())
            .Returns(new MockAsyncEnumerable<ModelDTO>(existingModels));
        
        // Act:
        _modelEvaluationService.Unsubscribe();
        await _blobStorageMonitorService.NotifySubscribersAsync();
        
        // Assert:
        // Check if the log messages written in ModelEvaluationServiceImpl after receiving the event
        // notification from the BlobStorageMonitorServiceImpl. This should NOT fire, if properly unsubscribed.
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Evaluation found best SoilHumidityPrediction model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never
        );
    }
    
    
    [Fact]
    public async Task HandleEventAsync_EvaluateSoilHumidityPredictionModelAndNotifiesBuildService_WhenNewModelsAddedEventIsFiredFromBlobStorageMonitorService() {
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
        
        // Subscribe to this class (ModelEvaluationService), to see if it properly fires its EvaluatedAllSoilHumidityPredictionModels event:
        EvaluatedAllSoilHumidityPredictionModelsEventArgs? eArg = null;
        _modelEvaluationService.AllSoilHumidityModelsEvaluated += (sender, e) => {
            eArg = e;
            return Task.CompletedTask;
        };
        
        
        // Act:
        await _blobStorageMonitorService.NotifySubscribersAsync();
        
        
        // Assert:
        // Check if the log messages written in ModelEvaluationServiceImpl after receiving the event
        // notification from the BlobStorageMonitorServiceImpl
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Evaluation found best SoilHumidityPrediction model")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );

        Assert.NotNull(eArg);
        Assert.Equal(betterModel.Type, eArg.BestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, eArg.BestModel.TrainingTimestamp);
        Assert.Equal(betterModel.R2, ((LinearRegressionModelDTO) eArg.BestModel).R2);
    }
}