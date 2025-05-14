using PredictionBuildService.core.ModelEntities;
using Xunit;

namespace PredictionBuildService.core.Tests.ModelEntities.Tests;

public class RandomForestModelDTOTests
{
    [Fact]
    public void ToString_ReturnsProperString_WhenConvertingDtoToString()
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

        string expectedToStringValue = "Type: " + type +
                                       ", Training_timestamp_utc: " + trainingTimestamp +
                                       "\n, DownloadUrl: " + null +
                                       "\n, target: " + target +
                                       "\n, feature_names: " + featuresNames +
                                       "\n, CrossValSplits: " + crossValSplits +
                                       "\n, RmseCV: " + rmseCv +
                                       "\n, R2: " + r2 +
                                       "\n, ComputedScore: " + computedScore;

        // Act + Assert:
        Assert.Equal(expectedToStringValue, modelDto.ToString());
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
