using PredictionBuildService.core.ModelEntities;
using Xunit;

namespace PredictionBuildService.core.Tests.ModelEntities.Tests;

public class RandomForestModelDTOTests
{

    [Fact]
    public void ToString_ReturnsNonEmptyString()
    {
        // Arrange
        var modelDto = new RandomForestModelDTO
        {
            Type = "RandomForest",
            TrainingTimestamp = "2025-05-07T07:04:41.641564",
            FeatureNames = new[] { "soil_humidity", "temperature" },
            Target = "minutes_to_dry"
        };

        // Act
        var result = modelDto.ToString();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void Copy_ReturnsCopyOfObject_WhenCalled()
    {
        // Arrange:
        var type = "RandomForest";
        var trainingTimestamp = "2025-05-07T07:04:41.641564";
        int crossValSplits = 5;
        string[] featuresNames = { "soil_humidity", "soil_delta", "air_humidity", "temperature", "light", "hour_sin", "hour_cos", "threshold" };
        double r2 = 0.59;
        double rmseCv = 394.93;
        double? computedScore = 0.81;
        var target = "minutes_to_dry (<20 % soil humidity)";

        var modelDto = new RandomForestModelDTO()
        {
            Type = type,
            TrainingTimestamp = trainingTimestamp,
            CrossValSplits = crossValSplits,
            FeatureNames = featuresNames,
            R2 = r2,
            RmseCv = rmseCv,
            Target = target,
            ComputedScore = computedScore
        };

        // Act:
        var copy = modelDto.Copy();

        // Assert:
        Assert.True(modelDto != copy);
        Assert.Equal(modelDto.Type, copy.Type);
        Assert.Equal(modelDto.TrainingTimestamp, copy.TrainingTimestamp);
        Assert.Equal(modelDto.CrossValSplits, copy.CrossValSplits);
        Assert.Equal(modelDto.FeatureNames, copy.FeatureNames);
        Assert.Equal(modelDto.R2, copy.R2);
        Assert.Equal(modelDto.RmseCv, copy.RmseCv);
        Assert.Equal(modelDto.Target, copy.Target);
        Assert.Equal(modelDto.DownloadUrl, copy.DownloadUrl);
        Assert.Equal(modelDto.ComputedScore, copy.ComputedScore);
    }
}
