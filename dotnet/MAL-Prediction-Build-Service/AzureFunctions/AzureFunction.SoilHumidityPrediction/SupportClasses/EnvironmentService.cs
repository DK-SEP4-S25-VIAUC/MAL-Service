using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class EnvironmentService : IEnvironmentService 
{
    public readonly string _environmentVariableName_OnnxBestSoilPredictionModelUri;

    public EnvironmentService(string environmentVariableName_OnnxBestSoilPredictionModelUri) {
        _environmentVariableName_OnnxBestSoilPredictionModelUri =
            environmentVariableName_OnnxBestSoilPredictionModelUri;
    }
    
    public string? GetEnvironmentVariable(string variable) => Environment.GetEnvironmentVariable(variable);

    public string GetEnvVarNameForBestOnnxSoilPredictionModelUri() {
        return _environmentVariableName_OnnxBestSoilPredictionModelUri;
    }
}