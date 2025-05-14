using Microsoft.Extensions.Logging;
using Sep4.PredictionApp.Interfaces;

namespace Sep4.PredictionApp.SupportClasses;

public class ModelLoader : IModelLoader {
    /// <summary>
    /// A cached instance of the ONNX model's inference session, shared across function invocations to improve performance.
    /// </summary>
    private static IInferenceSession? _cachedSession;
    
    /// <summary>
    /// The URI of the ONNX model in Azure Blob Storage, cached to detect changes in the model URI.
    /// </summary>
    private static string? _cachedUri;
    
    // Interfaces / Injected classes:
    private readonly ILogger<ModelLoader> _logger;
    private readonly IBlobDownloader _blobDownloader;
    private readonly IEnvironmentService _envService;
    private readonly IModelSessionFactory _sessionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelLoader"/> class.
    /// </summary>
    /// <param name="logger"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// <param name="envService"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// <param name="blobDownloader"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    /// <param name="modelSessionFactory"> is automatically injected into the constructor using dotnet dependency inversion principles.</param>
    public ModelLoader(ILogger<ModelLoader> logger, IBlobDownloader blobDownloader, IEnvironmentService envService, IModelSessionFactory modelSessionFactory) {
        _logger = logger;
        _blobDownloader = blobDownloader;
        _envService = envService;
        _sessionFactory = modelSessionFactory;
    }


    public async Task<IInferenceSession> GetOrLoadModelAsync() {
        try {
            string? onnxUri = _envService.GetEnvironmentVariable(_envService.GetEnvVarNameForBestOnnxSoilPredictionModelUri());
            if (string.IsNullOrEmpty(onnxUri))
                throw new InvalidOperationException($"{_envService.GetEnvVarNameForBestOnnxSoilPredictionModelUri()} is not set in configuration.");

            // Download the specified model to a temp directory:
            if (_cachedSession == null || _cachedUri != onnxUri) {
                _logger.LogInformation("Loading ONNX model...");

                // Downloads to a temporary directory, with the name 'model.onnx'.
                string tempFilePath = Path.Combine(Path.GetTempPath(), "soilPredictionModel.onnx");
                await _blobDownloader.DownloadAsync(onnxUri, tempFilePath);

                _cachedSession = _sessionFactory.Create(tempFilePath);

                _cachedUri = onnxUri;

                _logger.LogInformation("ONNX model loaded and cached.");
            }

            return _cachedSession;
        }
        catch (InvalidOperationException ex) {
            _logger.LogCritical(
                "{_envService.GetEnvVarNameForBestOnnxSoilPredictionModelUri()} is not set in configuration. Set it in Azure Function configuration (environment variables).", _envService.GetEnvVarNameForBestOnnxSoilPredictionModelUri());
            throw new InvalidOperationException(ex.Message);
        } catch (FileNotFoundException ex) {
            _logger.LogError(
                "No Prediction model found at location specified in azure environment variable. Please ensure that a model exists.");
            throw new FileNotFoundException(ex.Message);
        } catch (Exception ex) {
            _logger.LogError(ex.Message);
            throw new Exception(ex.Message);
        }

    }
}
