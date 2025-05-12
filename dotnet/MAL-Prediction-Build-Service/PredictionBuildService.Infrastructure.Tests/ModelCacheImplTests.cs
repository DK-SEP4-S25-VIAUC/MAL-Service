using System.Data;
using Moq;
using Microsoft.Extensions.Logging;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.Infrastructure.Tests;

/// <summary>
/// Automated Unit Testing for the class with the same name (with the 'Tests' part).
/// </summary>
public class ModelCacheImplTests : IDisposable
{
    private readonly Mock<ILogger<ModelCacheImpl>> _mockLogger;
    private ModelCacheImpl? _modelCacheImpl;
    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public ModelCacheImplTests() {
        _mockLogger = new Mock<ILogger<ModelCacheImpl>>();
        _modelCacheImpl = new ModelCacheImpl(_mockLogger.Object);
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
    
    
    // Test AddModelAsync:
    [Fact]
    public async Task AddModelAsync_ReturnsTrue_WhenValidModelDTOisProvided() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };

        // Act:
        var boolean = await _modelCacheImpl.AddModelAsync(model);
        
        // Assert:
        Assert.True(boolean);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentNullException_WhenTypeIsNull() {
        
        // Arrange:
        string? type = null;
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("type", exception.ParamName);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentNullException_WhenTimestampIsNull() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = null;
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("version", exception.ParamName);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentNullException_WhenGivenNullModelDTO() {
        
        // Arrange
        ModelDTO? model = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("newModelDto", exception.ParamName);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentException_WhenTypeIsEmpty() {
        
        // Arrange:
        string? type = "";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("type", exception.ParamName);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentException_WhenTimestampIsEmpty() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("version", exception.ParamName);
    }
        
    [Fact]
    public async Task AddModelAsync_ThrowsDuplicateNameException_WhenAddingAModelThatAlreadyExistsInTheCache() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act:
        await _modelCacheImpl.AddModelAsync(model);
        

        // Assert:
        await Assert.ThrowsAsync<DuplicateNameException>(() => _modelCacheImpl.AddModelAsync(model));
    }
    
    
    // Test RemoveModelAsync:
    [Fact]
    public async Task RemoveModelAsync_ReturnsTrue_WhenModelWasRemovedSuccessfullyFromCache() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act:
        await _modelCacheImpl.AddModelAsync(model);
        

        // Assert:
        bool success = await _modelCacheImpl.RemoveModelAsync(model.Type, model.TrainingTimestamp);
        Assert.True(success);
    }
    
    [Fact]
    public async Task RemoveModelAsync_ReturnsFalse_WhenAttemptingToRemoveModelFromCacheThatWasNeverAdded() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act:
        bool failure = await _modelCacheImpl.RemoveModelAsync(model.Type, model.TrainingTimestamp);
        

        // Assert:
        Assert.False(failure);
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelTypeArgumentIsNull() {
        
        // Arrange:
        string? type = null;
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.TrainingTimestamp));
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelTypeArgumentIsEmpty() {
        
        // Arrange:
        string? type = "";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.TrainingTimestamp));
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelTimestampArgumentIsNull() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = null;
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.TrainingTimestamp));
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelVersionArgumentIsEmpty() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.TrainingTimestamp));
    }
    
    
    // Test UpdateModelAsync:
    [Fact]
    public async Task UpdateModelAsync_ReturnsTrue_WhenModelIsUpdatedSuccessfully() {
        // Arrange:
        var oldModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-05-05T19:55:53.716723",
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 183.29807108324337,
            CrossValSplits = 5,
            RmseCv = 394.93,
            R2 = 0.59
        };
        
        var newModel = new LinearRegressionModelDTO {
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 124.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };

        await _modelCacheImpl.AddModelAsync(oldModel);
        
        
        // Act:
        bool success = await _modelCacheImpl.UpdateModelAsync(oldModel, newModel);
        var updatedModel = await _modelCacheImpl.FindModelAsync(newModel.Type, newModel.TrainingTimestamp);
        
        // Assert:
        Assert.True(success);
        Assert.Equal(newModel.TrainingTimestamp, updatedModel.TrainingTimestamp);
    }
    
    [Fact]
    public async Task UpdateModelAsync_ReturnsFalse_WhenOldModelToUpdateIsNotFoundInCache() {
        // Arrange:
        var oldModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-05-05T19:55:53.716723",
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 183.29807108324337,
            CrossValSplits = 5,
            RmseCv = 394.93,
            R2 = 0.59
        };
        
        var newModel = new LinearRegressionModelDTO {
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 124.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        
        // Act:
        bool failure = await _modelCacheImpl.UpdateModelAsync(oldModel, newModel);
        
        
        // Assert:
        Assert.False(failure);
    }
    
    [Fact]
    public async Task UpdateModelAsync_ThrowsArgumentNullException_WhenOldModelArgumentIsNull() {
        // Arrange:
        ModelDTO? oldModel = null;
        
        var newModel = new LinearRegressionModelDTO {
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 124.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.UpdateModelAsync(oldModel, newModel));
    }
    
    [Fact]
    public async Task UpdateModelAsync_ThrowsArgumentNullException_WhenNewModelArgumentIsNull() {
        // Arrange:
        var oldModel = new LinearRegressionModelDTO {
            Type = "Ridge (linear)",
            TrainingTimestamp = "2025-05-05T19:55:53.716723",
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 183.29807108324337,
            CrossValSplits = 5,
            RmseCv = 394.93,
            R2 = 0.59
        };
        
        ModelDTO? newModel = null;
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.UpdateModelAsync(oldModel, newModel));
    }
    
    
    // Test ListModelsAsync:
    [Fact]
    public void ListModelsAsync_ReturnsEmptyList_WhenModelCacheIsEmpty() {
        // Arrange:
        // Empty.
        
        
        // Act:
        var models = _modelCacheImpl.ListModelsAsync();
        
        
        // Assert:
        Assert.Empty(models);
    }
    
    [Fact]
    public async Task ListModelsAsync_ReturnsListWith1ModelDto_WhenModelCacheContains1ModelDto() {
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
            DownloadUrl = "https://example.com/test.onnx",
            Alpha = 124.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        
        // Act:
        await _modelCacheImpl.AddModelAsync(model1);

        var count = 0;
        await foreach (var model in _modelCacheImpl.ListModelsAsync()) {
            count++;
        }

        
        // Assert:
        Assert.Equal(1, count);
    }
    
    [Fact]
    public async Task ListModelsAsync_ReturnsListWith20ModelDto_WhenModelCacheContains20ModelDto() {
        // Arrange:
        List<ModelDTO> models = new List<ModelDTO>();
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "1";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        for (int i = 0; i < 20; i++) {
            var model = new LinearRegressionModelDTO {
                Type = type,
                TrainingTimestamp = trainingTimestampUtc,
                Target = target,
                FeatureNames = featureNames,
                DownloadUrl = downloadUrl,
                Alpha = alpha,
                CrossValSplits = crossValSplits,
                RmseCv = rmseCv,
                R2 = r2
            };
            trainingTimestampUtc += "24";
            models.Add(model);
        }
        
        
        // Act:
        foreach (var model in models) {
            await _modelCacheImpl.AddModelAsync(model);
        }
        
        int count = 0;
        await foreach (var model in _modelCacheImpl.ListModelsAsync()) {
            count++;
        }

        
        // Assert:
        Assert.Equal(20, count);
    }
    
    
    // Test FindModelAsync:
    [Fact]
    public async Task FindModelAsync_ReturnsFoundModel_WhenModelIsInCache() {
        
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        await _modelCacheImpl.AddModelAsync(model);
        
        
        // Act:
        var foundModel = await _modelCacheImpl.FindModelAsync(model.Type, model.TrainingTimestamp);
        

        // Assert:
        Assert.Equal(model.ToString(), foundModel.ToString());
    }
    
    [Fact]
    public async Task FindModelAsync_ThrowsKeyNotFoundException_WhenModelCannotBeFoundInCache() {
        
        // Arrange:
        // Arrange:
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "2025-05-05T19:55:53.716723";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        var model = new LinearRegressionModelDTO {
            Type = type,
            TrainingTimestamp = trainingTimestampUtc,
            Target = target,
            FeatureNames = featureNames,
            DownloadUrl = downloadUrl,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            RmseCv = rmseCv,
            R2 = r2
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _modelCacheImpl.FindModelAsync(model.Type, model.TrainingTimestamp));
    }
    
    
    // Test CacheSize:
    [Fact]
    public async Task CacheSize_Returns0_WhenModelCacheIsEmpty() {
        // Arrange:
        // Empty.
        
        
        // Act:
        int count = _modelCacheImpl.CacheSize();

        
        // Assert:
        Assert.Equal(0, count);
    }
    
    [Fact]
    public async Task CacheSize_Returns10_WhenModelCacheContains10Models() {
        // Arrange:
        List<ModelDTO> models = new List<ModelDTO>();
        string? type = "Ridge (linear)";
        string? trainingTimestampUtc = "1";
        string[]? featureNames = {
            "soil_humidity",
            "soil_delta",
            "air_humidity",
            "temperature",
            "light",
            "hour_sin",
            "hour_cos"
        };
        string? target = "minutes_to_dry (<20 % soil humidity)";
        string? downloadUrl = "https://example.com/test.onnx";
        double? alpha = 183.29807108324337;
        int? crossValSplits = 5;
        double? rmseCv = 394.93;
        double? r2 = 0.59;

        for (int i = 0; i < 10; i++) {
            var model = new LinearRegressionModelDTO {
                Type = type,
                TrainingTimestamp = trainingTimestampUtc,
                Target = target,
                FeatureNames = featureNames,
                DownloadUrl = downloadUrl,
                Alpha = alpha,
                CrossValSplits = crossValSplits,
                RmseCv = rmseCv,
                R2 = r2
            };
            trainingTimestampUtc += "24";
            models.Add(model);
        }
        
        
        // Act:
        foreach (var model in models) {
            await _modelCacheImpl.AddModelAsync(model);
        }
        
        int count = 0;
        await foreach (var model in _modelCacheImpl.ListModelsAsync()) {
            count++;
        }

        
        // Assert:
        Assert.Equal(10, count);
    }
    
}