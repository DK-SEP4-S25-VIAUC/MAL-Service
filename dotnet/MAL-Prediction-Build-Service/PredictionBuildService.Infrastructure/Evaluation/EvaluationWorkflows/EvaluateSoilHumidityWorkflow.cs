using PredictionBuildService.core.Interfaces;
using PredictionBuildService.core.ModelEntities;

namespace PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

/// <summary>
/// Holds the implementation logic for evaluating the best available model in prediction the future soil humidity metrics.
/// </summary>
public class EvaluateSoilHumidityWorkflow : IEvaluationWorkflow
{
    // _max_realistic_minutes_until_dry defines a realistic timespan in which a humid soil can realistically become dry.
    // It's initially set to: (60 min / hour) * (24 hours / day) * (7 days / week) = 10080 minutes.
    private readonly double _maxRealisticMinutesUntilDry;
    
    // _rmseUpperPenaltyLimit defines the upper limit of reasonable variance in predicted minutes_to_dry.
    // It's initially set to: sqrt(_maxRealisticMinutesUntilDry) = 100.4, meaning mean-variance above 100.4 minutes in
    // prediction error will be penalized.
    private readonly double _rmseUpperPenaltyLimit;

    // _r2OptimalLowerBoundary defines the lower limit of reasonable R2 (explained variance) level in percent.
    // // It's initially set to: 70%, meaning explained variance below 70% will be penalized due to underfitting.
    private readonly double _r2OptimalLowerBoundary;
    
    // _r2OptimalUpperBoundary defines the upper limit of reasonable R2 (explained variance) level in percent.
    // It's initially set to: 90%, meaning explained variance above 90% will be penalized due to overfitting.
    private readonly double _r2OptimalUpperBoundary;

    // _weightRmse defines how much the RMSE should influence the final score.
    private readonly double _weightRmse;

    // _weightR2 defines how much the R2 should influence the final score.
    private readonly double _weightR2;

    // _lowCrossValidationPenalty defines how much the final score should be reduced to (i.e. multiply by .25),
    // due to low cross validation values.
    private readonly double _lowCrossValidationPenalty;

    public EvaluateSoilHumidityWorkflow() {
        _maxRealisticMinutesUntilDry = 10080;
        _rmseUpperPenaltyLimit = double.Sqrt(_maxRealisticMinutesUntilDry);
        _r2OptimalLowerBoundary = 0.7;
        _r2OptimalUpperBoundary = 0.9;
        _weightRmse = 0.6;
        _weightR2 = 0.4;
        _lowCrossValidationPenalty = 0.25;
    }
    
    
    // TODO: Test
    // 1. Does it actually pick the 'best' model?
    // 2. What if it couldn't pick a model? (I.e. maybe there isn't a model available?)
    public async Task<ModelDTO> ExecuteEvaluationAsync(List<ModelDTO> soilPredictionModels) {
        return await Task.Run(() => {
            // Uses scoring to evaluate the best SoilPredictionModel on multiple parameters.
            // Arranges each 'modelType' (i.e. LinearRegressionModel, RandomForestModel, etc.) into ordered lists
            // and scores each based on their metrics compared to each other.
            // Then compares the best model from each main category, to find the overall best one.
        
            // ComputedScore LinearRegressionModel:
            // Extract all LinearRegressionModels and score these individually:
            var linearRegressionModels = soilPredictionModels
                .Where(m => m is LinearRegressionModelDTO)
                .Cast<LinearRegressionModelDTO>()
                .ToList();

            ModelDTO bestLinearRegressionModel = FindBestLinearRegressionSoilPredictionModel(linearRegressionModels);

            // ComputedScore other model types below:
            // TODO: Implement another step that evaluates which model, from several different model types, is the best.
            // i.e. if we add a RandomForest prediction model, which is better? The Linear Model or the RandomForest?
            // They both have different performance metrics that must be evaluated against each other.
        
            return bestLinearRegressionModel;
        });
    }

    private LinearRegressionModelDTO FindBestLinearRegressionSoilPredictionModel(List<LinearRegressionModelDTO> linearRegressionModels) {
        var scoredLinearRegressionModels = new Dictionary<LinearRegressionModelDTO, double>();

        foreach (var linearRegressionModel in linearRegressionModels) {
            // ComputedScore the model and add to scored models list:
            linearRegressionModel.ComputedScore = ComputeLinearRegressionSoilPredictionModelScore(
                linearRegressionModel.RmseCv!.Value,
                linearRegressionModel.R2!.Value,
                linearRegressionModel.CrossValSplits!.Value,
                _maxRealisticMinutesUntilDry,
                _rmseUpperPenaltyLimit,
                _r2OptimalUpperBoundary,
                _r2OptimalLowerBoundary,
                _weightRmse,
                _weightR2,
                _lowCrossValidationPenalty
            );
            
            scoredLinearRegressionModels.Add(
                linearRegressionModel, 
                linearRegressionModel.ComputedScore.Value
            );
        }
        
        // Order models by score:
        var modelsOrderedByScore = scoredLinearRegressionModels
            .OrderByDescending(entry => entry.Value)
            .Select(entry => entry.Key)
            .ToList();
        
        // Return the best model:
        return modelsOrderedByScore.First();
    }

    
    private double ComputeLinearRegressionSoilPredictionModelScore(
        double rmse, 
        double r2,
        int crossValSplits,
        double maxRealisticMinutesUntilDry,
        double rmseUpperPenaltyLimit,
        double r2OptimalUpperBoundary,
        double r2OptimalLowerBoundary,
        double weightRmse,
        double weightR2,
        double lowCrossValidationPenalty) {
        
        // Reward low root mean squared errors, and penalize large values. Since target is minutes_to_dry, we will
        // assume that a normal time until soil moisture becomes very dry happens within a week (7 day) period, without watering.
        // Thus, we assume a very bad RMSE would be (60 min / hour) * (24 hours / day) * (7 days / week) = 10080 minutes.
        // RSME variance above sqrt(10080) = 100.4 minutes will be penalized (meaning if evaluations are more than
        // 100 minutes off, on average, a penalty will be applied to the model). Values below this are rewarded, down until a 20-minute accuracy.
        double scoreFromRmse = 0;
        
        if (rmse > rmseUpperPenaltyLimit ) {
            // Penalize: (rmse/max_realistic_minutes_until_dry) * 100%
            // Applies a penalty in a linear increasing manner, based on how much larger RMSE is above the tolerable boundary.
            scoreFromRmse -= ((rmse/maxRealisticMinutesUntilDry)*100);
        } else if (rmse <= rmseUpperPenaltyLimit) {
            // Reward: (100% - (rmse/max_realisctic_minutes_until_dry) * 100%).
            // Applies a reward in a linear increasing manner, based on how close to 0% variance we get.
            scoreFromRmse += (100-((rmse/maxRealisticMinutesUntilDry)*100));
        }
        
        // Reward higher values of r2, which explains how much of the variance (in percentage), is explained by the model.
        // The ideal R2 will be estimated to be 90%, striking a balance between underfitting, optimal fitting and overfitting.
        // Values larger than 90% will be penalized heavily. Values lower than 70% will also be penalized heavily.
        double scoreFromR2 = 0;

        if (r2 > .9) {
            // Penalize: ((r2 - 90% lower boundary) / 10%) * 100% * 2
            // Applies penalty in a linear increasing manner, based on a spread where r2 = 0.90001 gets the
            // least penalty and 1.0 gets the largest penalty (0.9 to 1.0 is 10%, but is calculated as 100% of the penalty range).
            // All penalties are doubled, to heavily penalize overfitting.
            scoreFromR2 -= ((r2 - r2OptimalUpperBoundary) / (1 - r2OptimalUpperBoundary)) * 100 * 2;
            
        } else if (r2 <= r2OptimalUpperBoundary && r2 >= r2OptimalLowerBoundary) {
            // Reward: ((r2 - 70% lower boundary) / 20%) * 100%
            // Applies reward in a linear increasing manner, based on a spread where r2 = 0.70001 gets the
            // least reward and 0.9 gets the largest reward (0.7 to 0.9 is 20%, but is calculated as 100% of the reward range).
            scoreFromR2 += (r2 - r2OptimalLowerBoundary) / (r2OptimalUpperBoundary - r2OptimalLowerBoundary) * 100;
            
        } else {
            // Penalize: ((70% lower boundary - r2) / 70%) * 100%
            // Applies penalty in a linear increasing manner, based on a spread where r2 = 0.0001 gets the
            // least penalty and 0.699999 gets the largest penalty (0.0 to 0.7 is 70%, but is calculated as 100% of the penalty range).
            // These penalties are NOT doubled, since they are spread over a larger range than value above 0.9.
            scoreFromR2 -= ((r2OptimalLowerBoundary - r2) / r2OptimalLowerBoundary) * 100;
        }
        
        // Compute score:
        double score = (scoreFromRmse * weightRmse) + (scoreFromR2 * weightR2);
        
        // Heavily penalize low cross validation splits, since this is indicative of potential unstable validation metrics:
        if (crossValSplits < 5) {
            if (score < 0) {
                score *= 1 + lowCrossValidationPenalty;
            } else {
                score *= lowCrossValidationPenalty;
            }
        }
        
        return score;
    }
}