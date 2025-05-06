using Azure.Storage.Blobs; 
using Microsoft.Azure.Functions.Worker.Http; 
using Microsoft.Extensions.Logging; 
using Microsoft.ML.OnnxRuntime; 
using Moq; 
using Sep4.PredictionApp; 
using Xunit;

namespace AzureFunction.SoilHumidityPrediction.tests;

public class PredictSoilHumidityTests
{
    private readonly Mock<ILogger<PredictSoilHumidity>> _loggerMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new ();
    private readonly Mock<InferenceSession> _inferenceSessionMock = new ();
    private readonly PredictSoilHumidity _function;
    
    [Fact]
    public async Task Run_ThrowsArgumentNullException_WhenHttpRequestIsNull()
    {
        // Arrange
        HttpRequestData? request = null;

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _function.Run(request!));
    }
}