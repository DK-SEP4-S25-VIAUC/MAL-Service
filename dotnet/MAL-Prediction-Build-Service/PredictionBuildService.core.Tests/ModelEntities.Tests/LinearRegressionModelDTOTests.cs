using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.core.Tests.ModelEntities.Tests;

public class LinearRegressionModelDTOTests
{
    [Fact]
    public void ToString_ReturnsProperString_WhenConvertingDtoToString() {
        // Arrange:
        var type = "Ridge (linear)";
        var trainingTimestamp = "2025-05-07T07:04:41.641564";
        double alpha = 183.29807108324337;
        int crossValSplits = 5;
        string[] featuresNames = {"soil_humidity","soil_delta","air_humidity","temperature","light","hour_sin","hour_cos","threshold"};
        double r2 = 0.59;
        double rmseCv = 394.93;
        var target = "minutes_to_dry (<20 % soil humidity)";
        
        var modelDto = new LinearRegressionModelDTO() {
            Type = type,
            TrainingTimestamp = trainingTimestamp,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            FeatureNames = featuresNames,
            R2 = r2,
            RmseCv = rmseCv,
            Target = target
        };

        string expectedToStringValue = "Type: " + type +
                                       ", Training_timestamp_utc: " + trainingTimestamp +
                                       "\n, DownloadUrl: " + null +
                                       "\n, target: " + target +
                                       "\n, feature_names: " + featuresNames +
                                       "\n, alpha: " + alpha +
                                       "\n, CrossValSplits: " + crossValSplits +
                                       "\n, RmseCV: " + rmseCv +
                                       "\n, R2: " + r2;
        
        // Act + Assert:
        Assert.Equal(expectedToStringValue, modelDto.ToString());
    }
    
    
    [Fact]
    public void Copy_ReturnsCopyOfObject_WhenCalled() {
        // Arrange:
        var type = "Ridge (linear)";
        var trainingTimestamp = "2025-05-07T07:04:41.641564";
        double alpha = 183.29807108324337;
        int crossValSplits = 5;
        string[] featuresNames = {"soil_humidity","soil_delta","air_humidity","temperature","light","hour_sin","hour_cos","threshold"};
        double r2 = 0.59;
        double rmseCv = 394.93;
        var target = "minutes_to_dry (<20 % soil humidity)";
        
        var modelDto = new LinearRegressionModelDTO() {
            Type = type,
            TrainingTimestamp = trainingTimestamp,
            Alpha = alpha,
            CrossValSplits = crossValSplits,
            FeatureNames = featuresNames,
            R2 = r2,
            RmseCv = rmseCv,
            Target = target
        };
        
        // Act:
        var copy = modelDto.Copy();
        
        // + Assert:
        Assert.True(modelDto != copy);
        Assert.Equal(modelDto.Type, copy.Type);
        Assert.Equal(modelDto.TrainingTimestamp, copy.TrainingTimestamp);
        Assert.Equal(modelDto.Alpha, copy.Alpha);
        Assert.Equal(modelDto.CrossValSplits, copy.CrossValSplits);
        Assert.Equal(modelDto.FeatureNames, copy.FeatureNames);
        Assert.Equal(modelDto.R2, copy.R2);
        Assert.Equal(modelDto.RmseCv, copy.RmseCv);
        Assert.Equal(modelDto.Target, copy.Target);
        Assert.Equal(modelDto.DownloadUrl, copy.DownloadUrl);
    }
}