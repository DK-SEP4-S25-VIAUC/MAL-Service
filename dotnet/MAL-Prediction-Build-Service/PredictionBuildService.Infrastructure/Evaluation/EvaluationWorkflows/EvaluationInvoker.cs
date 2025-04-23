using System.Reflection;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

public class EvaluationInvoker
{
    private readonly Dictionary<string, IEvaluationWorkflow> _evaluationModels = new();

    public EvaluationInvoker() {
        // Register all available (and future) evaluation models/logic below:
        // IMPORTANT: Please add any command in ALL LOWERCASE (i.e.: 'evaluatesoilhumiditymodel')!
        _evaluationModels.Add("evaluatesoilhumiditymodel", FindEvaluationModelType<EvaluateSoilHumidityWorkflow>() ?? throw new FileNotFoundException());
    }
    
    private IEvaluationWorkflow FindEvaluationModelType<TModel>() where TModel : IEvaluationWorkflow {
        // We check through the EvaluateWorkflows folder and discover all C# class types:
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes()
            .Where(t => t.Namespace == "PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows"
                        && typeof(IEvaluationWorkflow).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract);

        // Find the first type that matches TModel:
        var targetType = types.FirstOrDefault(t => t == typeof(TModel));

        if (targetType != null) {
            // Instantiate the type
            return (IEvaluationWorkflow)Activator.CreateInstance(targetType);
        } else {
            throw new InvalidOperationException($"No type found for {typeof(TModel).Name} in EvaluationWorkflows namespace.");
        }
    }

    public IEvaluationWorkflow GetEvaluationWorkflow(string command) {
        // Check if the model associated with this command exists:
        string commandLowerCase = command.ToLower();

        if (_evaluationModels.ContainsKey(commandLowerCase)) {
            return new EvaluateSoilHumidityWorkflow();
        } else {
            throw new InvalidDataException($"No evaluation model associated with command='{commandLowerCase}' could be found!");
        }
    }
}