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
    private ModelCacheImpl _modelCacheImpl;
    
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
        string? type = "LinearRegression";
        string? version = "1.0.0";

        var model = new ModelDTO {
            Type = type,
            Version = version
        };


        // Act:
        var boolean = await _modelCacheImpl.AddModelAsync(model);
        
        // Assert:
        Assert.True(boolean);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentNullException_WhenTypeIsNull() {
        
        // Arrange
        string? type = null;
        string? version = "1.0.0";

        var model = new ModelDTO {
            Type = type,
            Version = version
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("type", exception.ParamName);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentNullException_WhenVersionIsNull() {
        
        // Arrange
        string? type = "LinearRegression";
        string? version = null;

        var model = new ModelDTO {
            Type = type,
            Version = version
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
        
        // Arrange
        string? type = "";
        string? version = "1.0.0";

        var model = new ModelDTO {
            Type = type,
            Version = version
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("type", exception.ParamName);
    }
    
    [Fact]
    public async Task AddModelAsync_ThrowsArgumentException_WhenVersionIsEmpty() {
        
        // Arrange
        string? type = "LinearRegression";
        string? version = "";

        var model = new ModelDTO {
            Type = type,
            Version = version
        };


        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.AddModelAsync(model));
        Assert.Equal("version", exception.ParamName);
    }
        
    [Fact]
    public async Task AddModelAsync_ThrowsDuplicateNameException_WhenAddingAModelThatAlreadyExistsInTheCache() {
        
        // Arrange:
        string? type = "LinearRegression";
        string? version = "1.0.0";

        var model = new ModelDTO {
            Type = type,
            Version = version
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
        string? type = "LinearRegression";
        string? version = "1.0.0";

        var model = new ModelDTO {
            Type = type,
            Version = version
        };
        
        
        // Act:
        await _modelCacheImpl.AddModelAsync(model);
        

        // Assert:
        bool success = await _modelCacheImpl.RemoveModelAsync(model.Type, model.Version);
        Assert.True(success);
    }
    
    [Fact]
    public async Task RemoveModelAsync_ReturnsFalse_WhenAttemptingToRemoveModelFromCacheThatWasNeverAdded() {
        
        // Arrange:
        var model = new ModelDTO { 
            Type = "LinearRegression",
            Version = "1.0.0"
        };
        
        
        // Act:
        bool failure = await _modelCacheImpl.RemoveModelAsync(model.Type, model.Version);
        

        // Assert:
        Assert.False(failure);
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelTypeArgumentIsNull() {
        
        // Arrange:
        var model = new ModelDTO { 
            Type = null,
            Version = "1.0.0"
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.Version));
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelTypeArgumentIsEmpty() {
        
        // Arrange:
        var model = new ModelDTO { 
            Type = "",
            Version = "1.0.0"
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.Version));
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelVersionArgumentIsNull() {
        
        // Arrange:
        var model = new ModelDTO { 
            Type = "LinearRegression",
            Version = null
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.Version));
    }
    
    [Fact]
    public async Task RemoveModelAsync_ThrowsArgumentException_WhenModelVersionArgumentIsEmpty() {
        
        // Arrange:
        var model = new ModelDTO { 
            Type = "LinearRegression",
            Version = ""
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentException>(() => _modelCacheImpl.RemoveModelAsync(model.Type, model.Version));
    }
    
    
    // Test UpdateModelAsync:
    [Fact]
    public async Task UpdateModelAsync_ReturnsTrue_WhenModelIsUpdatedSuccessfully() {
        // Arrange:
        var oldModel = new ModelDTO { 
             Type = "LinearRegression", 
             Version = "1.0.0" 
        };
        
        var newModel = new ModelDTO { 
            Type = "LinearRegression", 
            Version = "1.0.1" 
        };

        await _modelCacheImpl.AddModelAsync(oldModel);
        
        
        // Act:
        bool success = await _modelCacheImpl.UpdateModelAsync(oldModel, newModel);
        var updatedModel = await _modelCacheImpl.FindModelAsync(newModel.Type, newModel.Version);
        
        // Assert:
        Assert.True(success);
        Assert.Equal(newModel.Version, updatedModel.Version);
    }
    
    [Fact]
    public async Task UpdateModelAsync_ReturnsFalse_WhenOldModelToUpdateIsNotFoundInCache() {
        // Arrange:
        var oldModel = new ModelDTO { 
            Type = "LinearRegression", 
            Version = "1.0.0" 
        };
        
        var newModel = new ModelDTO { 
            Type = "LinearRegression", 
            Version = "1.0.1" 
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
        
        var newModel = new ModelDTO { 
            Type = "LinearRegression", 
            Version = "1.0.1" 
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<ArgumentNullException>(() => _modelCacheImpl.UpdateModelAsync(oldModel, newModel));
    }
    
    [Fact]
    public async Task UpdateModelAsync_ThrowsArgumentNullException_WhenNewModelArgumentIsNull() {
        // Arrange:
        ModelDTO? oldModel = new ModelDTO { 
            Type = "LinearRegression", 
            Version = "1.0.0" 
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
        var model1 = new ModelDTO { 
            Type = "LinearRegression", 
            Version = "1.0.0"
        };
        
        
        // Act:
        await _modelCacheImpl.AddModelAsync(model1);

        int count = 0;
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
        string type = "LinearRegression";
        string version = "1";

        for (int i = 0; i < 20; i++)
        {
            var model = new ModelDTO
            {
                Type = type,
                Version = version
            };
            version += ".0";
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
        string? type = "LinearRegression";
        string? version = "1.0.0";

        var model = new ModelDTO {
            Type = type,
            Version = version
        };
        
        await _modelCacheImpl.AddModelAsync(model);
        
        
        // Act:
        var foundModel = await _modelCacheImpl.FindModelAsync(model.Type, model.Version);
        

        // Assert:
        Assert.Equal(model.ToString(), foundModel.ToString());
    }
    
    [Fact]
    public async Task FindModelAsync_ThrowsKeyNotFoundException_WhenModelCannotBeFoundInCache() {
        
        // Arrange:
        var model = new ModelDTO { 
            Type = "LinearRegression",
            Version = "1.0.0"
        };
        
        
        // Act + Assert:
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _modelCacheImpl.FindModelAsync(model.Type, model.Version));
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
        string type = "LinearRegression";
        string version = "1";

        for (int i = 0; i < 10; i++)
        {
            var model = new ModelDTO
            {
                Type = type,
                Version = version
            };
            version += ".0";
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