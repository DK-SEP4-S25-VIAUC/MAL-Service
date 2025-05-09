using Azure.Storage.Blobs;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PredictionBuildService.Configurations;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Tests.Monitoring;

/// <summary>
/// Automated Unit Testing for the class with the same name (with the 'Tests' part).
/// </summary>
public class BlobStorageMonitorServiceImplTests
{
    private readonly Mock<ILogger<ModelCacheImpl>> _mockLogger;
    private ModelCacheImpl _modelCacheImpl;
    private readonly Mock<IBlobStorageMonitorService> _mockMonitorService;
    private readonly Mock<IOptions<AzureBlobStorageSettings>> _mockSettings;
    private readonly Mock<IModelCache> _mockModelCache;
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<IBlobStorageInteractionHelper> _mockBlobStorageInteractionHelperMock;
    private readonly Mock<IModelEvaluationService> _mockModelEvaluationServiceMock;
    private readonly Mock<IBuildService> _mockBuildService;
    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public BlobStorageMonitorServiceImplTests() {
        _mockLogger = new Mock<ILogger<ModelCacheImpl>>();
        _modelCacheImpl = new ModelCacheImpl(_mockLogger.Object);
        _mockMonitorService = new Mock<IBlobStorageMonitorService>();
        _mockSettings = new Mock<IOptions<AzureBlobStorageSettings>>();
        _mockModelCache = new Mock<IModelCache>();
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockBlobStorageInteractionHelperMock = new Mock<IBlobStorageInteractionHelper>();
        _mockModelEvaluationServiceMock = new Mock<IModelEvaluationService>();
        _mockBuildService = new Mock<IBuildService>();
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() {
        _modelCacheImpl = null;
    }
    
    
}