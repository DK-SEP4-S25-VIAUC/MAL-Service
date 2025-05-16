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
    // It's initially set to: 70%, meaning explained variance below 70% will be penalized due to underfitting.
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

    public async Task<ModelDTO> ExecuteEvaluationAsync(List<ModelDTO> soilPredictionModels) {
        return await Task.Run(() => {
            // Uses scoring to evaluate the best SoilPredictionModel on multiple parameters.
            // Arranges each 'modelType' (i.e. LinearRegressionModel, RandomForestModel, etc.) into ordered lists
            // and scores each based on their metrics compared to each other.
            // Then compares the best model from each main category, to find the overall best one.

            // 1. Score linear regression models
            var linearModels = soilPredictionModels
                .OfType<LinearRegressionModelDTO>()
                .ToList();

            LinearRegressionModelDTO? bestLinearModel = null;

            if (linearModels.Any()) {
                var scoredLinearRegressionModels = new Dictionary<LinearRegressionModelDTO, double>();

                foreach (var model in linearModels) {
                    double score = ComputeModelScore(
                        model.RmseCv!.Value,
                        model.R2!.Value,
                        model.CrossValSplits!.Value
                    );
                    model.ComputedScore = score;
                    scoredLinearRegressionModels.Add(model, score);
                }

                var modelsOrderedByScore = scoredLinearRegressionModels
                    .OrderByDescending(entry => entry.Value)
                    .Select(entry => entry.Key)
                    .ToList();

                bestLinearModel = modelsOrderedByScore.First();
            }

            // 2. Score random forest models
            var forestModels = soilPredictionModels
                .OfType<RandomForestModelDTO>()
                .ToList();

            RandomForestModelDTO? bestForestModel = null;

            if (forestModels.Any()) {
                var scoredForestModels = new Dictionary<RandomForestModelDTO, double>();

                foreach (var model in forestModels) {
                    double score = ComputeModelScore(
                        model.RmseCv!.Value,
                        model.R2!.Value,
                        model.CrossValSplits!.Value
                    );
                    model.ComputedScore = score;
                    scoredForestModels.Add(model, score);
                }

                var modelsOrderedByScore = scoredForestModels
                    .OrderByDescending(entry => entry.Value)
                    .Select(entry => entry.Key)
                    .ToList();

                bestForestModel = modelsOrderedByScore.First();
            }

            // 3. After finding bestLinearModel and bestForestModel with known scores:
            var candidates = new Dictionary<ModelDTO, double>();

            if (bestLinearModel != null)
                candidates[bestLinearModel] = bestLinearModel.R2!.Value; 

            if (bestForestModel != null)
                candidates[bestForestModel] = bestForestModel.R2!.Value;  

            return candidates
                .OrderByDescending(kv => kv.Value)
                .Select(kv => kv.Key)
                .FirstOrDefault() ?? throw new InvalidOperationException("No valid models found.");
        });
    }

    private double ComputeModelScore(double rmse, double r2, int crossValSplits) {
        double scoreFromRmse = 0;

        if (rmse > _rmseUpperPenaltyLimit) {
            scoreFromRmse -= ((rmse / _maxRealisticMinutesUntilDry) * 100);
        } else {
            scoreFromRmse += (100 - ((rmse / _maxRealisticMinutesUntilDry) * 100));
        }

        double scoreFromR2 = 0;

        if (r2 > _r2OptimalUpperBoundary) {
            scoreFromR2 -= ((r2 - _r2OptimalUpperBoundary) / (1 - _r2OptimalUpperBoundary)) * 100 * 2;
        } else if (r2 >= _r2OptimalLowerBoundary) {
            scoreFromR2 += (r2 - _r2OptimalLowerBoundary) / (_r2OptimalUpperBoundary - _r2OptimalLowerBoundary) * 100;
        } else {
            scoreFromR2 -= ((_r2OptimalLowerBoundary - r2) / _r2OptimalLowerBoundary) * 100;
        }

        double score = (scoreFromRmse * _weightRmse) + (scoreFromR2 * _weightR2);

        if (crossValSplits < 5) {
            score = score < 0 ? score * (1 + _lowCrossValidationPenalty) : score * _lowCrossValidationPenalty;
        }

        return score;
    }
}
