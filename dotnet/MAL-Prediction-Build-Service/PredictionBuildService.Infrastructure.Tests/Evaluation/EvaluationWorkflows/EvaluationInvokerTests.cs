using PredictionBuildService.core.Interfaces;
using PredictionBuildService.Infrastructure.Evaluation.EvaluationWorkflows;

namespace PredictionBuildService.Infrastructure.Tests.Evaluation.EvaluationWorkflows;

public class EvaluationInvokerTests {
    
    /// <summary>
    /// In XUnit, the constructor is run before each test, acting as a "SetUp()" method. 
    /// </summary>
    /// <remarks>
    /// Similar to "beforeEach()" from JUnit.
    /// </remarks>
    public EvaluationInvokerTests() {
        // Empty
    }
    
    /// <summary>
    /// In DotNet, the IDisposable interface gives access to the "Dispose()" method, which acts as a tearDown method after each test.
    /// </summary>
    /// <remarks>
    /// Similar to "afterEach()" from JUnit.
    /// </remarks>
    public void Dispose() {
        // Empty
    }
    
    
    [Fact]
    public void EvaluationInvoker_AddsEvaluateSoilHumidityWorkflowToCommandDictionary_WhenEvaluateSoilHumidityWorkflowClassExistsInSameDirectoryAsEvaluationInvoker() {
        // Arrange:
        var evaluationInvoker = new EvaluationInvoker();
        
        
        // Act:
        bool workflowAddedToDictionary;
        IEvaluationWorkflow? workflow = null;
        try {
            workflow = evaluationInvoker.GetEvaluationWorkflow("EvaluateSoilHumidity");
            workflowAddedToDictionary = true;
        } catch (InvalidDataException ignored) {
            workflowAddedToDictionary = false;
        }

        
        // Assert:
        Assert.True(workflowAddedToDictionary);
        Assert.NotNull(workflow);
        Assert.Equal(typeof(EvaluateSoilHumidityWorkflow), workflow.GetType());
    }
    
}