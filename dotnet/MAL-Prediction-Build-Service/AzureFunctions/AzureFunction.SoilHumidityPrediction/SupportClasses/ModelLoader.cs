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
    
    // Interfaces / Injected cclasses:
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

    /// <summary>
    /// Loads the ONNX model from Azure Blob Storage or returns the cached instance if already loaded.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="IInferenceSession"/>
    /// object representing the loaded ONNX model.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <c>OnnxModelUri</c> environment variable is not set.
    /// </exception>
    /// <exception cref="Azure.RequestFailedException">
    /// Thrown if the ONNX model cannot be downloaded from Azure Blob Storage.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if there is an error writing the model file to the temporary path.
    /// </exception>
    /// <remarks>
    /// The ONNX model is cached locally in the Function App to avoid reloading it for each function invocation.
    /// The model is reloaded if the <c>OnnxModelUri</c> environment variable changes.
    /// </remarks>
    public async Task<IInferenceSession> GetOrLoadModelAsync() {
        try {
            string? onnxUri = _envService.GetEnvironmentVariable("OnnxModelUri");
            if (string.IsNullOrEmpty(onnxUri))
                throw new InvalidOperationException("OnnxModelUri is not set in configuration.");

            // Download the specified model to a temp directory:
            if (_cachedSession == null || _cachedUri != onnxUri) {
                _logger.LogInformation("Loading ONNX model...");

                // Downloads to a temporary directory, with the name 'model.onnx'.
                string tempFilePath = Path.Combine(Path.GetTempPath(), "model.onnx");
                await _blobDownloader.DownloadAsync(onnxUri, tempFilePath);

                _cachedSession = _sessionFactory.Create(tempFilePath);

                _cachedUri = onnxUri;

                _logger.LogInformation("ONNX model loaded and cached.");
            }

            return _cachedSession;
        }
        catch (InvalidOperationException ex) {
            _logger.LogCritical(
                "OnnxModelUri is not set in configuration. Set it in Azure Function configuration (environment variables).");
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
