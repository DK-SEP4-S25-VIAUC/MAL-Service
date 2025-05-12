namespace Sep4.PredictionApp.Interfaces;

public interface IEnvironmentService
{
    string? GetEnvironmentVariable(string variable);
    string GetEnvVarNameForBestOnnxSoilPredictionModelUri();
}