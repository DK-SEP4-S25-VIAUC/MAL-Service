using NUnit.Framework;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using API.Services.PredictionService;
using API.Services.SensorDataService;
using API.DataEntities;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace API.Tests
{
    [TestFixture]
public class PredictionServiceTests
{
    private Mock<HttpMessageHandler> _mockHttpHandler;
    private HttpClient _httpClient;
    private Mock<ISensorDataService> _mockSensorDataService;
    private IConfiguration _mockConfiguration;
    private PredictionService _service;

    [SetUp]
    public void Setup()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();

        _httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri("https://example.com/api/")
        };

        _mockSensorDataService = new Mock<ISensorDataService>();

        var configData = new Dictionary<string, string>
        {
            { "PredictionService:SoilHumidityApiKey", "fake-key" }
        };
        _mockConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new PredictionService(_httpClient, _mockConfiguration, _mockSensorDataService.Object);
    }

    [Test]
    public async Task GetPredictionAsync_ReturnsNull_WhenSamplesAreMissing()
    {
        _mockSensorDataService.Setup(s => s.getSamples(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new OkObjectResult(new List<SampleDTO>()));

        var result = await _service.GetPredictionAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetPredictionAsync_Throws_WhenHttpFails()
    {
        var samples = new List<SampleDTO>
        {
            new() { timestamp = DateTime.UtcNow, soil_humidity = 30, air_humidity = 40, air_temperature = 20, light_value = 100 },
            new() { timestamp = DateTime.UtcNow.AddMinutes(-1), soil_humidity = 31, air_humidity = 41, air_temperature = 21, light_value = 105 }
        };

        _mockSensorDataService.Setup(s => s.getSamples(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new OkObjectResult(samples));

        _mockSensorDataService.Setup(s => s.getSoilHumiLowerThresholdAsync()).ReturnsAsync(12.0);

        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _service.GetPredictionAsync();
        });
    }

    [Test]
    public void GetPredictionAsync_Throws_WhenResponseHasNoNumber()
    {
        var samples = new List<SampleDTO>
        {
            new() { timestamp = DateTime.UtcNow, soil_humidity = 30, air_humidity = 40, air_temperature = 20, light_value = 100 },
            new() { timestamp = DateTime.UtcNow.AddMinutes(-1), soil_humidity = 31, air_humidity = 41, air_temperature = 21, light_value = 105 }
        };

        _mockSensorDataService.Setup(s => s.getSamples(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new OkObjectResult(samples));

        _mockSensorDataService.Setup(s => s.getSoilHumiLowerThresholdAsync()).ReturnsAsync(12.0);

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _service.GetPredictionAsync();
        });
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }
}
}
