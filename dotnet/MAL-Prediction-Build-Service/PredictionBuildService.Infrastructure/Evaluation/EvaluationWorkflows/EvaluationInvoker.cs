using System.Reflection;
using PredictionBuildService.core.Interfaces;

namespace PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

/// <summary>
/// Contains a list of all valid evaluation methods corresponding to each prediction model type. Exposes methods to interact with these evaluation methods,
/// allowing for another class to i.e. call the evaluation logic/workflow required to properly evaluate a LinearRegressionModel.
/// </summary>
/// <remarks>
/// This invoker class is part of the Command Pattern implementation
/// </remarks>
public class EvaluationInvoker
{
    private readonly Dictionary<string, IEvaluationWorkflow> _evaluationModels = new();

    /// <summary>
    /// Primary constructor. Takes no external arguments. Automatically registers all workflows defined in classes located in the same folder as this class. It takes the filename, strips the 'Workflow' part and uses that as the key/command.
    /// <br />
    /// With a workflow class called 'EvaluateSoilHumidityWorkflow' the corresponding command to get this workflow is 'EvaluateSoilHumidity'.
    /// </summary>
    public EvaluationInvoker() {
        var assembly = Assembly.GetExecutingAssembly();

        var workflowTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows"
                        && typeof(IEvaluationWorkflow).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract);

        foreach (var type in workflowTypes) {
            var instance = Activator.CreateInstance(type) as IEvaluationWorkflow;
            if (instance == null) {
                throw new InvalidOperationException($"Could not instantiate type {type.Name}");
            }
            
            var nameWithoutSuffix = type.Name.EndsWith("Workflow")
                ? type.Name[..^"Workflow".Length]
                : type.Name;

            var key = nameWithoutSuffix.ToLowerInvariant();
            if (_evaluationModels.ContainsKey(key)) {
                throw new InvalidOperationException($"Duplicate workflow key detected: '{key}'.");
            }

            _evaluationModels.Add(key, instance);
        }
    }

    
    /// <summary>
    /// Looks up and returns the specified model evaluation workflow, if such a workflow exists.
    /// </summary>
    /// <param name="command">The workflow to look up, specified by a command (i.e. EvaluateSoilHumidityModels</param>
    /// <returns>The model implementing the logic associated with the specified command, allowing for calling class to execute the given evaluation workflow.</returns>
    /// <exception cref="InvalidDataException">Thrown if the provided command is not a recognized command.</exception>
    public IEvaluationWorkflow GetEvaluationWorkflow(string command) {
        // Check if the model associated with this command exists:
        string commandLowerCase = command.ToLower();

        if (_evaluationModels.TryGetValue(commandLowerCase, out var workflow)) {
            return workflow;
        } else {
            throw new InvalidDataException($"No evaluation model associated with command='{commandLowerCase}' could be found!");
        }
    }
}