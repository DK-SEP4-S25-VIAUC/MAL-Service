using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class EnvironmentService : IEnvironmentService
{
    public string? GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);
}