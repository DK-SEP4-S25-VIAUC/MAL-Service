namespace PredictionBuildService;

public class Program
{
    
    public static void Main(string[] args)
    {
        // 1. Continuously monitor Azure Blob Storage for Changes.
        // This is done with Azure Event Grid.
        
        // 2. Process the Change Event:
        // When a new or updated ONNX model is detected, extract metadata (e.g., model type, version) from the blob’s name or metadata.
        
        // 3. Build and Deploy Azure Functions:
        // Generate or update an Azure Function project dynamically (e.g., a C# Function App) to use the new ONNX model for predictions.
        // Deploy the Azure Function to Azure using the Azure Resource Manager (ARM) SDK
        
        // 4. Run as a Worker Service
        // Use a C# Worker Service to host the logic, listening for events and performing deployments.
        // Integrate with Azure services using the Azure SDK.
        
        
        
        
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        host.RunAsync();
    }
}