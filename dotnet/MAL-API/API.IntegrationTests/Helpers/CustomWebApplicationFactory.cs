using API.Services.PredictionService;
using API.Services.SensorDataService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace API.IntegrationTests.Helpers;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    public Mock<IPredictionService> MockPredictionService { get; } = new();
    public Mock<ISensorDataService> MockSensorDataService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
         
            var predictionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IPredictionService));
            if (predictionDescriptor != null)
                services.Remove(predictionDescriptor);

           
            var sensorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ISensorDataService));
            if (sensorDescriptor != null)
                services.Remove(sensorDescriptor);
            
            services.AddSingleton(MockPredictionService.Object);
            services.AddSingleton(MockSensorDataService.Object);
        });
    }
}