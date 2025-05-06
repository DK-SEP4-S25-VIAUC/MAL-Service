using Azure.Storage.Blobs;
using Moq;
using PredictionBuildService.core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.Tests;

/// <summary>
/// Automated Unit Testing for the class with the same name (with the 'Tests' part).
/// </summary>
public class WorkerTests
{
    /// <summary>
    /// Tests if the main worker thread launches successfully, calling the expected methods and is capable of a gentle shutdown.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_LaunchesSuccessfully_CallsExpectedMethods() {
        // Arrange:
        var monitorServiceMock = new Mock<IBlobStorageMonitorService>();
        var loggerMock = new Mock<ILogger<Worker>>();
        var settingsMock = new Mock<IOptions<AzureBlobStorageSettings>>();
        var modelCacheMock = new Mock<IModelCache>();
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var blobStorageInteractionHelperMock = new Mock<IBlobStorageInteractionHelper>();
        var modelEvaluationServiceMock = new Mock<IModelEvaluationService>();
        var buildServiceMock = new Mock<IBuildService>();
        
        // Setup settings:
        var settings = new AzureBlobStorageSettings {
            ContainerName = "test-container",
            ModelMetaDataFormat = ".json",
            ModelFileType = ".onnx"
        };
        settingsMock.Setup(s => s.Value).Returns(settings);
        
        // Setup cache to simulate empty cache:
        modelCacheMock.Setup(m => m.CacheSize()).Returns(0);
        modelCacheMock.Setup(m => m.ListModelsAsync()).Returns(GetEmptyAsyncEnumerable<ModelDTO>());
        
        // Setup monitor service to complete immediately
        monitorServiceMock.Setup(m => m.MonitorAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();
        
        // Setup cancellation token:
        var cts = new CancellationTokenSource();

        // Setup logger to verify log messages:
        loggerMock.Setup(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("PredictionBuildService started at")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));

        // Create Worker instance:
        var worker = new Worker(
            monitorServiceMock.Object,
            loggerMock.Object,
            settingsMock.Object,
            modelCacheMock.Object,
            blobServiceClientMock.Object,
            blobStorageInteractionHelperMock.Object,
            modelEvaluationServiceMock.Object,
            buildServiceMock.Object);
        
        
        
        // Act:
        var startTask = worker.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await worker.StopAsync(cts.Token);
        await startTask;

        
        
        // Assert:
        // Verify logger called for start:
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("PredictionBuildService started at")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once());

        // Verify cache was checked and models were loaded:
        modelCacheMock.Verify(m => m.CacheSize(), Times.Once());
        blobStorageInteractionHelperMock.Verify(
            b => b.LoadAllModelsIntoCacheAsync(
                blobServiceClientMock.Object,
                It.IsAny<CancellationToken>(),
                settings.ContainerName,
                settings.ModelMetaDataFormat,
                settings.ModelFileType),
            Times.Once());

        // Verify model listing was called:
        modelCacheMock.Verify(m => m.ListModelsAsync(), Times.Once());

        // Verify monitoring service was notified and started:
        monitorServiceMock.Verify(m => m.NotifySubscribersAsync(), Times.Once());
        monitorServiceMock.Verify(m => m.MonitorAsync(It.IsAny<CancellationToken>()), Times.Once());

        // Verify unsubscribe calls on shutdown:
        modelEvaluationServiceMock.Verify(m => m.Unsubscribe(), Times.Once());
        buildServiceMock.Verify(b => b.Unsubscribe(), Times.Once());
    }
    
    /// <summary>
    /// Helper method to create an empty IAsyncEnumerable:
    /// </summary>
    private static async IAsyncEnumerable<T> GetEmptyAsyncEnumerable<T>()
    {
        yield break;
    }
}