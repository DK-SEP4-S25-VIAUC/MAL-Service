using PredictionBuildService.core.Interfaces;
using PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;
using PredictionBuildService.core.ModelEntities;
using Xunit.Abstractions;

namespace PredictionBuildService.Infrastructure.Tests.Evaluation.EvaluationWorkflows;

/// <summary>
/// Automated Unit Testing for the class with the same name (with the 'Tests' part).
/// </summary>
public class EvaluateSoilHumidityWorkflowTests : IDisposable
{
    private IEvaluationWorkflow? _evaluateSoilHumidityWorkflow;
    private ITestOutputHelper _output;
    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public EvaluateSoilHumidityWorkflowTests(ITestOutputHelper output) {
        _evaluateSoilHumidityWorkflow = new EvaluateSoilHumidityWorkflow();
        _output = output;
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() {
        _evaluateSoilHumidityWorkflow = null;
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasHigherCrossValSplitsAndLowerRmseCVAndHigherR2Values() {
        // Arrange:
        var modelWithLowerCrossVallSplitsAndHigherRmseCvAndLowR2 = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 3,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        var modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2 = new LinearRegressionModelDTO {
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

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(modelWithLowerCrossVallSplitsAndHigherRmseCvAndLowR2);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(modelWithLowerCrossVallSplitsAndHigherRmseCvAndLowR2);
        
        models.Add(modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(modelWithLowerCrossVallSplitsAndHigherRmseCvAndLowR2); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2.Type, bestModel.Type);
        Assert.Equal(modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2.RmseCv, bestModel.RmseCv);
        Assert.Equal(modelWithHigherCrossVallSplitsAndLowerRmseCvAndHigherR2.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasSameCrossValSplitsAndLowerRmseCVAndHigherR2Values() {
        // Arrange:
        var worseModelWithSameCrossValSplitsAndHigherRmseCvAndLowerR2 = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
            RmseCv = 252.93,
            R2 = 0.42
        };
        
        var betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values = new LinearRegressionModelDTO {
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

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModelWithSameCrossValSplitsAndHigherRmseCvAndLowerR2);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModelWithSameCrossValSplitsAndHigherRmseCvAndLowerR2);
        
        models.Add(betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModelWithSameCrossValSplitsAndHigherRmseCvAndLowerR2); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values.Type, bestModel.Type);
        Assert.Equal(betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModelWithSameCrossValSplitsAndLowerRmseCvAndHigherR2Values.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasSameCrossValSplitsAndSameRmseCVAndHigherR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
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
            RmseCv = 252.93,
            R2 = 0.85
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasHigherCrossValSplitsAndSameRmseCVAndSameR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 4,
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
            RmseCv = 252.93,
            R2 = 0.42
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasSameCrossValSplitsAndLowerRmseCVAndSameR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
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
            RmseCv = 240,
            R2 = 0.42
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasSameCrossValSplitsAndHigherRmseCVAndHigherR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
            RmseCv = 240,
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
            RmseCv = 260,
            R2 = 0.45
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasHigherCrossValSplitsAndLowerRmseCVAndLowerR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 4,
            RmseCv = 240,
            R2 = 0.45
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
            CrossValSplits = 6,
            RmseCv = 200,
            R2 = 0.40
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasHigherCrossValSplitsAndHigherRmseCVAndLowerR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 4,
            RmseCv = 200,
            R2 = 0.45
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
            CrossValSplits = 6,
            RmseCv = 240,
            R2 = 0.40
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasHigherCrossValSplitsAndHigherRmseCVAndHigherR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 4,
            RmseCv = 200,
            R2 = 0.45
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
            RmseCv = 202,
            R2 = 0.46
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
    
    [Fact]
    public async Task ExecuteEvaluationAsync_PicksBestModel_WhenEvaluatingTwoModelsWhereBetterModelHasLowerCrossValSplitsAndHigherRmseCVAndHigherR2Values() {
        // Arrange:
        var worseModel = new LinearRegressionModelDTO {
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
            Alpha = 76.29807108324337,
            CrossValSplits = 5,
            RmseCv = 200,
            R2 = 0.45
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
            CrossValSplits = 4,
            RmseCv = 250,
            R2 = 0.55
        };

        List<ModelDTO> models = new List<ModelDTO>();
        models.Add(worseModel);
        _output.WriteLine($"Bad Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Remove(worseModel);
        
        models.Add(betterModel);
        _output.WriteLine($"\nBetter Model ComputedScore = {await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models)}");
        models.Add(worseModel); ;
        
        
        // Act:
        LinearRegressionModelDTO bestModel = (LinearRegressionModelDTO) await _evaluateSoilHumidityWorkflow!.ExecuteEvaluationAsync(models);
        
        
        // Assert:
        Assert.NotNull(bestModel);
        Assert.Equal(betterModel.Type, bestModel.Type);
        Assert.Equal(betterModel.TrainingTimestamp, bestModel.TrainingTimestamp);
        Assert.Equal(betterModel.CrossValSplits, bestModel.CrossValSplits);
        Assert.Equal(betterModel.RmseCv, bestModel.RmseCv);
        Assert.Equal(betterModel.R2, bestModel.R2);
    }
    
}