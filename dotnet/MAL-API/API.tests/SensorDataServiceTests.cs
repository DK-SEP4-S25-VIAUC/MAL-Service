using NUnit.Framework;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using API.Services.SensorDataService;
using API.DataEntities;
using System.Text.Json;
using System.Collections.Generic;

namespace API.Tests
{
    [TestFixture]
    public class SensorDataServiceTests
    {
        private Mock<HttpMessageHandler> _mockHandler;
        private HttpClient _httpClient;
        private Mock<ILogger<SensorDataService>> _mockLogger;
        private SensorDataService _service;

        [SetUp]
        public void Setup()
        {
            _mockHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHandler.Object)
            {
                BaseAddress = new Uri("http://localhost/api/")
            };

            _mockLogger = new Mock<ILogger<SensorDataService>>();
            _service = new SensorDataService(_httpClient, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient.Dispose();
        }

        [Test]
        public async Task getSoilHumiLowerThresholdAsync_ReturnsValue_WhenFlatResponseIsValid()
        {
            var json = """
            {
              "lowerbound": 42.0,
              "upperbound": 80.0
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var result = await _service.getSoilHumiLowerThresholdAsync();
            Assert.That(result, Is.EqualTo(42.0));
        }

        [Test]
        public async Task GetLatestSoilHumidityValueAsync_ReturnsValue_WhenResponseIsValid()
        {
            var json = """
            {
              "SoilHumidityDTO": {
                "id": 1,
                "time_stamp": "2025-03-23T14:30:00Z",
                "soil_humidity_value": 45.2
              }
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var result = await _service.GetLatestSoilHumidityValueAsync();
            Assert.That(result, Is.EqualTo(45.2));
        }

        [Test]
        public async Task getSoilHumiLowerThresholdAsync_ReturnsFallback_WhenApiFails()
        {
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var result = await _service.getSoilHumiLowerThresholdAsync();
            Assert.That(result, Is.EqualTo(20.0)); // Fallback
        }

        [Test]
        public async Task getSamples_ReturnsOk_WhenApiReturnsValidSamples()
        {
            var json = """
            {
              "list": [
                { "SampleDTO": {
                    "timestamp": "2025-03-20T14:30:00Z",
                    "soil_humidity": 45.5,
                    "air_humidity": 55.2,
                    "air_temperature": 22.3,
                    "light_value": 3000.0
                  }},
                { "SampleDTO": {
                    "timestamp": "2025-03-20T14:25:00Z",
                    "soil_humidity": 44.0,
                    "air_humidity": 54.0,
                    "air_temperature": 21.0,
                    "light_value": 2800.0
                  }}
              ]
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var result = await _service.getSamples(null, null);
            Assert.That(result, Is.InstanceOf<OkObjectResult>());

            var okResult = result as OkObjectResult;
            var data = okResult!.Value as List<SampleDTO>;
            Assert.That(data?.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public async Task getSamples_ReturnsNotFound_WhenLessThanTwoSamples()
        {
            var json = """
            {
              "list": [
                { "SampleDTO": {
                    "timestamp": "2025-03-20T14:30:00Z",
                    "soil_humidity": 45.5,
                    "air_humidity": 55.2,
                    "air_temperature": 22.3,
                    "light_value": 3000.0
                  }}
              ]
            }
            """;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            var result = await _service.getSamples(null, null);
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task getSamples_ReturnsFallback_WhenExceptionThrown()
        {
            _mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulated failure"));

            var result = await _service.getSamples(null, null);
            Assert.That(result, Is.InstanceOf<OkObjectResult>()); // fallbackList used
        }
    }
}
