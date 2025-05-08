namespace PredictionBuildService.core.EventArgs;

/// <summary>
/// A basic EventArg that is used to provide Observer pattern integration in the application.
/// This class is intended to be used to signal that a better Linear Regression Model was identified, built and is now ready for deployment to Azure Functions.
/// </summary>
public class BuiltLinearRegressionModelForDeploymentEventArgs : System.EventArgs
{
    // Class is deliberately empty.
}