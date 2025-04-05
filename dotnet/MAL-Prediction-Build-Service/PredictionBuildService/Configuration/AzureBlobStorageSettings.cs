namespace PredictionBuildService.Configuration;

/// <summary>
/// Contains properties for Azure Blob Storage settings, defined in the appsettings.json file.
/// This allows for dependency injection and usage across all classes in the project.
/// </summary>
public class AzureBlobStorageSettings
{
    public string ContainerName { get; set; } = string.Empty;
    public string StorageAccountUri { get; set; } = string.Empty;
    public string QueueUri { get; set; } = string.Empty;
    public string ModelFileType { get; set; } = string.Empty;
    public string ModelMetaDataFormat { get; set; } = string.Empty;
}