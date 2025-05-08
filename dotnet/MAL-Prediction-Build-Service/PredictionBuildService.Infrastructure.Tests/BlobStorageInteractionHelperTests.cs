using Microsoft.Extensions.Logging;
using Moq;
using Azure.Storage.Blobs;
using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;

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

    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public BlobStorageInteractionHelperTests() {
        _blobStorageInteractionHelper = new BlobStorageInteractionHelperImpl(_mockLogger.Object, _mockModelCache.Object);
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
        Assert.Throws<Newtonsoft.Json.JsonReaderException>(() => 
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
}