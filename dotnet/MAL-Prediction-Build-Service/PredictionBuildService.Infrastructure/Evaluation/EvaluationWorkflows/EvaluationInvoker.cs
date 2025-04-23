using System.Reflection;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

public class EvaluationInvoker
{
    private readonly Dictionary<string, IEvaluateModel> _evaluationModels = new();

    public EvaluationInvoker() {
        // Register all available (and future) evaluation models/logic below:
        // IMPORTANT: Please add any command in ALL LOWERCASE (i.e.: 'evaluatesoilhumiditymodel')!
        _evaluationModels.Add("evaluatesoilhumiditymodel", FindEvaluationModelType<EvaluateSoilHumidityModel>() ?? throw new FileNotFoundException());
    }
    
    private IEvaluateModel FindEvaluationModelType<TModel>() where TModel : IEvaluateModel {
        // We check through the EvaluateWorkflows folder and discover all C# class types:
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes()
            .Where(t => t.Namespace == "PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows"
                        && typeof(IEvaluateModel).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract);

        // Find the first type that matches TModel:
        var targetType = types.FirstOrDefault(t => t == typeof(TModel));

        if (targetType != null) {
            // Instantiate the type
            return (IEvaluateModel)Activator.CreateInstance(targetType);
        } else {
            throw new InvalidOperationException($"No type found for {typeof(TModel).Name} in EvaluationWorkflows namespace.");
        }
    }

    public IEvaluateModel GetEvaluationModel(string command) {
        // Check if the model associated with this command exists:
        string commandLowerCase = command.ToLower();

        if (_evaluationModels.ContainsKey(commandLowerCase)) {
            return new EvaluateSoilHumidityModel();
        } else {
            throw new InvalidDataException($"No evaluation model associated with command='{commandLowerCase}' could be found!");
        }
    }
}