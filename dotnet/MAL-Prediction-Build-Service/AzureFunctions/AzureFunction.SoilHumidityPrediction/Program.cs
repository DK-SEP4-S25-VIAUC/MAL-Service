using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sep4.PredictionApp;
using Sep4.PredictionApp.Interfaces;
using Sep4.PredictionApp.SupportClasses;

var builder = FunctionsApplication.CreateBuilder(args);

// Register dependencies for dependency injection:
builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
builder.Services.AddSingleton<IEnvironmentService>(e => new EnvironmentService("OnnxBestSoilPredictionModelUri"));
builder.Services.AddSingleton<IBlobDownloader, BlobDownloader>();
builder.Services.AddSingleton<IModelSessionFactory, ModelSessionFactory>();
builder.Services.AddSingleton<IModelLoader, ModelLoader>();
builder.Services.AddSingleton<PredictSoilHumidity>();

// Set up the Azure Functions worker:
builder.ConfigureFunctionsWebApplication();

// Run it:
builder.Build().Run();