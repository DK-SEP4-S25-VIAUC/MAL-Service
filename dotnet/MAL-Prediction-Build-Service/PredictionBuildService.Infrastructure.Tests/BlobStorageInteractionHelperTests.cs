using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;
using PredictionBuildService.Infrastructure.Tests.HelperClasses;
using Xunit.Abstractions;

namespace PredictionBuildService.Infrastructure.Tests;

/// <summary>
/// Automated Unit Testing for the class with the same name (with the 'Tests' part).
/// </summary>
public class BlobStorageInteractionHelperTests : IDisposable
{
    private readonly Mock<ILogger<BlobStorageInteractionHelperImpl>> _mockLogger = new ();
    private Mock<IModelCache> _mockModelCache = new ();
    private IBlobStorageInteractionHelper? _blobStorageInteractionHelper;
    private readonly string _azureBlobStorage__StorageAccountUri = "https://modelregistrymal.blob.core.windows.net/";
    private readonly string _azureBlobStorage__QueueUri = "https://modelregistrymal.queue.core.windows.net/blob-events";
    private readonly string _azureBlobStorage__ContainerName = "models";
    private readonly string _azureBlobStorage__ModelFileType = ".onnx";
    private readonly string _azureBlobStorage__ModelMetaDataFormat = ".metadata.json";
    private readonly ITestOutputHelper _output;

    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public BlobStorageInteractionHelperTests(ITestOutputHelper output) {
        _blobStorageInteractionHelper = new BlobStorageInteractionHelperImpl(_mockLogger.Object, _mockModelCache.Object);
        _output = output;
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() {
        // Reset BlobStorageInteractionHelper between each test.
        _blobStorageInteractionHelper = null;
    }
    
    [Fact]
    public void ConvertFromJsonMetadataToModelDTO_ReturnsModelDTO_WhenValidLinearRegressionModelMetadataIsReceivedAsParameter()
    {
        // Arrange:
        var json = @"{
            ""model_type"": ""Ridge (Linear)"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [
                ""soil_humidity"",
                ""soil_delta"",
                ""air_humidity"",
                ""temperature"",
                ""light"",
                ""hour_sin"",
                ""hour_cos"",
                ""threshold""
            ],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock.SetupGet(b => b.Uri).Returns(new Uri(_azureBlobStorage__StorageAccountUri + _azureBlobStorage__ContainerName + "/model" + _azureBlobStorage__ModelMetaDataFormat));
        
        // Act:
        var result = _blobStorageInteractionHelper.ConvertFromJsonMetadataToModelDTO(json, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType, blobClientMock.Object);

        
        // Assert:
        Assert.NotNull(result);
        Assert.IsType<LinearRegressionModelDTO>(result);
        Assert.Equal(_azureBlobStorage__StorageAccountUri + _azureBlobStorage__ContainerName + "/model" + _azureBlobStorage__ModelFileType, result.DownloadUrl);
    }

    
    [Fact]
    public void ConvertFromJsonMetadataToModelDTO_ThrowsJsonReaderException_WhenInvalidJsonIsPassedToMethod()
    {
        // Arrange:
        string invalidJson = "{model_type: Ridge (Linear)"; // Malformed JSON
        var blobClient = new Mock<BlobClient>().Object;

        // Act + Assert:
        Assert.Throws<JsonReaderException>(() => 
            _blobStorageInteractionHelper.ConvertFromJsonMetadataToModelDTO(invalidJson, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType, blobClient));
    }

    
    [Fact]
    public void ConvertFromJsonMetadataToModelDTO_ThrowsMissingFieldException_WhenModelTypeParameterIsMissing()
    {
        // Arrange:
        string jsonWithoutModelType = @"{
            ""NOT_model_type"": ""NOT PROPERLY SET"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [
                ""soil_humidity"",
                ""soil_delta"",
                ""air_humidity"",
                ""temperature"",
                ""light"",
                ""hour_sin"",
                ""hour_cos"",
                ""threshold""
            ],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";
        var blobClient = new Mock<BlobClient>().Object;

        // Act + Assert:
        var ex = Assert.Throws<MissingFieldException>(() =>
            _blobStorageInteractionHelper.ConvertFromJsonMetadataToModelDTO(jsonWithoutModelType, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType, blobClient));

        Assert.Contains("model_type", ex.Message);
    }


    [Fact]
    public void ConvertFromJsonMetadataToModelDTO_ThrowsFormatException_WhenUnrecognizedModelTypeIsPassedAsParameter()
    {
        // Arrange:
        string json = @"{
            ""model_type"": ""This is an unrecognized model type"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [
                ""soil_humidity"",
                ""soil_delta"",
                ""air_humidity"",
                ""temperature"",
                ""light"",
                ""hour_sin"",
                ""hour_cos"",
                ""threshold""
            ],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";
        var blobClient = new Mock<BlobClient>().Object;

        // Act & Assert
        var ex = Assert.Throws<FormatException>(() =>
            _blobStorageInteractionHelper.ConvertFromJsonMetadataToModelDTO(json, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType, blobClient));

        Assert.Contains("model_type", ex.Message);
    }
    
    
    [Fact]
    public async Task ListAllBlobsAsync_ReturnsBlobNames_WhenBlobsExist() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();

        var blobs = new[] {
            BlobsModelFactory.BlobItem(name: "LinearRegressionSoilPredictionModel1" + _azureBlobStorage__ModelFileType),
            BlobsModelFactory.BlobItem(name: "LinearRegressionSoilPredictionModel1" + _azureBlobStorage__ModelMetaDataFormat),
            BlobsModelFactory.BlobItem(name: "LinearRegressionSoilPredictionModel2" + _azureBlobStorage__ModelFileType),
            BlobsModelFactory.BlobItem(name: "LinearRegressionSoilPredictionModel2" + _azureBlobStorage__ModelMetaDataFormat)
        };

        var asyncPageableMock = MockAsyncPageable(blobs);

        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), Mock.Of<Response>()));

        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(asyncPageableMock);

        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        var token = CancellationToken.None;

        // Act:
        var result = await _blobStorageInteractionHelper.ListAllBlobsAsync(blobServiceClientMock.Object, _azureBlobStorage__ContainerName, token);

        
        // Assert:
        Assert.Equal(4, result.Count);
        Assert.Contains("LinearRegressionSoilPredictionModel1" + _azureBlobStorage__ModelFileType, result);
        Assert.Contains("LinearRegressionSoilPredictionModel2" + _azureBlobStorage__ModelFileType, result);
        Assert.Contains("LinearRegressionSoilPredictionModel1" + _azureBlobStorage__ModelMetaDataFormat, result);
        Assert.Contains("LinearRegressionSoilPredictionModel2" + _azureBlobStorage__ModelMetaDataFormat, result);
    }

    [Fact]
    public async Task ListAllBlobsAsync_ReturnsEmptyList_WhenContainerIsEmpty() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();

        var asyncPageableMock = MockAsyncPageable<BlobItem>(Enumerable.Empty<BlobItem>());

        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));

        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(asyncPageableMock);

        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
            .Returns(containerClientMock.Object);

        var token = CancellationToken.None;

        // Act:
        var result = await _blobStorageInteractionHelper.ListAllBlobsAsync(blobServiceClientMock.Object, _azureBlobStorage__ContainerName, token);

        // Assert:
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListAllBlobsAsync_ThrowsException_WhenContainerNameIsInvalid() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();

        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(It.IsAny<string>()))
            .Throws(new Exception("Invalid container name"));

        var token = CancellationToken.None;

        // Act + Assert:
        await Assert.ThrowsAsync<Exception>(() => _blobStorageInteractionHelper.ListAllBlobsAsync(blobServiceClientMock.Object, "??invalid", token));
    }
    
    
    [Fact]
    public async Task LoadAllModelsIntoCacheAsync_ConsoleLogsSuccess_WhenTryingToLoadValidModelMetadataFromAzureBlobStorage() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        string json = @"{
            ""model_type"": ""Ridge (Linear)"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [""soil_humidity"", ""soil_delta"", ""air_humidity"", ""temperature"", ""light"", ""hour_sin"", ""hour_cos"", ""threshold""],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";

        string blobName = "model1" + _azureBlobStorage__ModelMetaDataFormat;
        var blobItems = new[] { BlobsModelFactory.BlobItem(name: blobName) };

        // Setup Mocks required for the ListAllBlobsAsync method call:
        // Reference call to the container:
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(_azureBlobStorage__ContainerName))
            .Returns(containerClientMock.Object);


        // Ensuring the container exists call:
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));


        // Listing the  blobs in the container
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItems));
        
        
        // Setup Mocks required / called directly inside LoadAllModelsIntoCacheAsync:
        containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(blobClientMock.Object);
        
        blobClientMock
            .Setup(b => b.Uri)
            .Returns(new Uri($"https://modelregistrymal.blob.core.windows.net/{_azureBlobStorage__ContainerName}/{blobName}"));

        var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(json));
        blobClientMock
            .Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blobDownloadResult, Mock.Of<Response>()));

        _mockModelCache
            .Setup(m => m.AddModelAsync(It.IsAny<ModelDTO>()))
            .ReturnsAsync(true);

        var token = CancellationToken.None;

        // Act:
        await _blobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(blobServiceClientMock.Object, token, _azureBlobStorage__ContainerName, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType);
        
        // Assert:
        // Verify model was added to modelCache:
        _mockModelCache.Verify(m => m.AddModelAsync(It.IsAny<ModelDTO>()), Times.Once(), "AddModelAsync was not called the expected number of times.");
        
        // Verify success message was logged:
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Completed loading existing models into local cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    
    [Fact]
    public async Task LoadAllModelsIntoCacheAsync_ConsoleLogsErrorOfMissingMetadataKeyAndValue_WhenTryingToLoadModelMetadataWithInvalidContent() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        string jsonWithoutModelType = @"{
            ""NOT_model_type"": ""NOT PROPERLY SET"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [""soil_humidity"", ""soil_delta"", ""air_humidity"", ""temperature"", ""light"", ""hour_sin"", ""hour_cos"", ""threshold""],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";

        string blobName = "model1" + _azureBlobStorage__ModelMetaDataFormat;
        var blobItems = new[] { BlobsModelFactory.BlobItem(name: blobName) };

        // Setup Mocks required for the ListAllBlobsAsync method call:
        // Reference call to the container:
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(_azureBlobStorage__ContainerName))
            .Returns(containerClientMock.Object);


        // Ensuring the container exists call:
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));


        // Listing the  blobs in the container
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItems));
        
        
        // Setup Mocks required / called directly inside LoadAllModelsIntoCacheAsync:
        containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(blobClientMock.Object);

        var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(jsonWithoutModelType));
        blobClientMock
            .Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blobDownloadResult, Mock.Of<Response>()));


        var token = CancellationToken.None;

        // Act:
        await _blobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(blobServiceClientMock.Object, token, _azureBlobStorage__ContainerName, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType);
        
        // Assert:
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("'model_type' is not defined in the provided json metadata.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    
    [Fact]
    public async Task LoadAllModelsIntoCacheAsync_ConsoleLogsJsonDeserializationFailure_WhenTryingToLoadModelMetadataIsNotJsonFormatted() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        string notJsonFormatted = @"{this/is-not___}{jsonFormatted}";

        string blobName = "model1" + _azureBlobStorage__ModelMetaDataFormat;
        var blobItems = new[] { BlobsModelFactory.BlobItem(name: blobName) };

        // Setup Mocks required for the ListAllBlobsAsync method call:
        // Reference call to the container:
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(_azureBlobStorage__ContainerName))
            .Returns(containerClientMock.Object);


        // Ensuring the container exists call:
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));


        // Listing the  blobs in the container
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItems));
        
        
        // Setup Mocks required / called directly inside LoadAllModelsIntoCacheAsync:
        containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(blobClientMock.Object);

        var blobDownloadResult = BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(notJsonFormatted));
        blobClientMock
            .Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(blobDownloadResult, Mock.Of<Response>()));


        var token = CancellationToken.None;

        // Act:
        await _blobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(blobServiceClientMock.Object, token, _azureBlobStorage__ContainerName, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType);
        
        // Assert:
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Could not deserialize into ModelDTO:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }
    
    
    [Fact]
    public async Task LoadAllModelsIntoCacheAsync_LogsCompletionMessageWithoutError_WhenLoadingModelsFromBlobThatIsEmpty() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();

        //string blobName = "model1.metadata.json";
        BlobItem[] blobItems = [];

        // Setup Mocks required for the ListAllBlobsAsync method call:
        // Reference call to the container:
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(_azureBlobStorage__ContainerName))
            .Returns(containerClientMock.Object);
        
        // Ensuring the container exists call:
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));

        // Listing the  blobs in the container
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItems));

        var token = CancellationToken.None;

        // Act:
        await _blobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(blobServiceClientMock.Object, token, _azureBlobStorage__ContainerName, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType);
        
        // Assert:
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Completed loading existing models into local cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    
    [Fact]
    public async Task LoadAllModelsIntoCacheAsync_LogsDownloadFailure_WhenDownloadFromAzureBlobStorageFails() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        string jsonWithoutModelType = @"{
            ""model_type"": ""Ridge (Linear)"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [""soil_humidity"", ""soil_delta"", ""air_humidity"", ""temperature"", ""light"", ""hour_sin"", ""hour_cos"", ""threshold""],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";

        string blobName = "model1" + _azureBlobStorage__ModelMetaDataFormat;
        var blobItems = new[] { BlobsModelFactory.BlobItem(name: blobName) };

        // Setup Mocks required for the ListAllBlobsAsync method call:
        // Reference call to the container:
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(_azureBlobStorage__ContainerName))
            .Returns(containerClientMock.Object);


        // Ensuring the container exists call:
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));


        // Listing the  blobs in the container
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItems));
        
        
        // Setup Mocks required / called directly inside LoadAllModelsIntoCacheAsync:
        containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(blobClientMock.Object);
        
        blobClientMock
            .Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Failed to download blob content"));
        
        var token = CancellationToken.None;

        // Act:
        await _blobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(blobServiceClientMock.Object, token, _azureBlobStorage__ContainerName, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType);
        
        // Assert:
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Error occurred while downloading")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    
    [Fact]
    public async Task LoadAllModelsIntoCacheAsync_HandlesDownloadTimeout_WhenDownloadFromAzureBlobStorageHangsIndefinitely() {
        // Arrange:
        var blobServiceClientMock = new Mock<BlobServiceClient>();
        var containerClientMock = new Mock<BlobContainerClient>();
        var blobClientMock = new Mock<BlobClient>();

        string json = @"{
            ""model_type"": ""Ridge (Linear)"",
            ""target"": ""minutes_to_dry (<20 % soil humidity)"",
            ""feature_names"": [""soil_humidity"", ""soil_delta"", ""air_humidity"", ""temperature"", ""light"", ""hour_sin"", ""hour_cos"", ""threshold""],
            ""alpha"": 183.29807108324337,
            ""cross_val_splits"": 5,
            ""training_timestamp_utc"": ""2025-05-07T07:04:41.641564"",
            ""rmse_cv"": 394.93,
            ""r2_insample"": 0.59
        }";

        string blobName = "slowModel1" + _azureBlobStorage__ModelMetaDataFormat;
        var blobItems = new[] { BlobsModelFactory.BlobItem(name: blobName) };

        // Setup Mocks required for the ListAllBlobsAsync method call:
        // Reference call to the container:
        blobServiceClientMock
            .Setup(b => b.GetBlobContainerClient(_azureBlobStorage__ContainerName))
            .Returns(containerClientMock.Object);


        // Ensuring the container exists call:
        containerClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<PublicAccessType>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContainerInfo(new ETag("etag"), DateTimeOffset.UtcNow), null!));


        // Listing the  blobs in the container
        containerClientMock
            .Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(MockAsyncPageable(blobItems));
        
        
        // Setup Mocks required / called directly inside LoadAllModelsIntoCacheAsync:
        containerClientMock
            .Setup(c => c.GetBlobClient(blobName))
            .Returns(blobClientMock.Object);
        
        blobClientMock
            .Setup(b => b.Uri)
            .Returns(new Uri($"https://modelregistrymal.blob.core.windows.net/{_azureBlobStorage__ContainerName}/{blobName}"));


        // This Mock simulates a task that never ends (until the cancellation token is invoked)
        blobClientMock
            .Setup(b => b.DownloadContentAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) => {
                var tcs = new TaskCompletionSource<Response>();
                ct.Register(() => tcs.TrySetCanceled());
                await tcs.Task;
                return Response.FromValue(BlobsModelFactory.BlobDownloadResult(BinaryData.FromString(json)), Mock.Of<Response>());
            });

        _mockModelCache
            .Setup(m => m.AddModelAsync(It.IsAny<ModelDTO>()))
            .ReturnsAsync(true);
        
        
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)); // Timeout after 100ms

        // Act:
        await Assert.ThrowsAsync<TaskCanceledException>(() => _blobStorageInteractionHelper.LoadAllModelsIntoCacheAsync(blobServiceClientMock.Object, cts.Token, _azureBlobStorage__ContainerName, _azureBlobStorage__ModelMetaDataFormat, _azureBlobStorage__ModelFileType));
        
        
        // Assert:
        // Verify model was not added to modelCache:
        _mockModelCache.Verify(m => m.AddModelAsync(It.IsAny<ModelDTO>()), Times.Never(), "AddModelAsync was unexpectedly called.");
        
        // Verify success message was logged:
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("Error occurred while downloading:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once
        );
    }

    
    // Helper to mock AsyncPageable<T>
    private static AsyncPageable<T> MockAsyncPageable<T>(IEnumerable<T> items){
        var mockPageable = new Mock<AsyncPageable<T>>();

        mockPageable
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new MockAsyncEnumerator<T>(items.GetEnumerator()));

        return mockPageable.Object;
    }
}