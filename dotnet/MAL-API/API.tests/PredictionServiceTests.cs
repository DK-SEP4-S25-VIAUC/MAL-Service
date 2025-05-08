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
        public async Task GetPredictionAsync_ReturnsForecastDTO_WhenValid()
        {
            // Arrange
            var latestTimestamp = DateTime.UtcNow;
            var samples = new List<SampleDTO>
            {
                new() { Timestamp = latestTimestamp, Soil_Humidity = 30, Air_Humidity = 40, Air_Temperature = 20, Light_Value = 100 },
                new() { Timestamp = latestTimestamp.AddMinutes(-10), Soil_Humidity = 32, Air_Humidity = 42, Air_Temperature = 21, Light_Value = 110 }
            };

            _mockSensorDataService.Setup(s => s.getSamples(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new OkObjectResult(samples));

            _mockSensorDataService.Setup(s => s.getSoilHumiLowerThresholdAsync()).ReturnsAsync(12.0);

            var responseContent = @"{""minutes_to_dry"": 45}";
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _service.GetPredictionAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That((result.next_watering_time - latestTimestamp).TotalMinutes, Is.EqualTo(45).Within(0.1));
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
                new() { Timestamp = DateTime.UtcNow, Soil_Humidity = 30, Air_Humidity = 40, Air_Temperature = 20, Light_Value = 100 },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(-1), Soil_Humidity = 31, Air_Humidity = 41, Air_Temperature = 21, Light_Value = 105 }
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
                new() { Timestamp = DateTime.UtcNow, Soil_Humidity = 30, Air_Humidity = 40, Air_Temperature = 20, Light_Value = 100 },
                new() { Timestamp = DateTime.UtcNow.AddMinutes(-1), Soil_Humidity = 31, Air_Humidity = 41, Air_Temperature = 21, Light_Value = 105 }
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
