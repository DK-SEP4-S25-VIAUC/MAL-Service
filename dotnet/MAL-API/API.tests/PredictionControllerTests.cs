using NUnit.Framework;
using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using API.Controllers;
using API.Services.PredictionService;
using API.Services.SensorDataService;
using API.DataEntities;

namespace API.Tests
{
    [TestFixture]
    public class PredictionControllerTests
    {
        private Mock<IPredictionService> _mockPredictionService;
        private Mock<ISensorDataService> _mockSensorDataService;
        private Mock<ILogger<PredictionController>> _mockLogger;
        private PredictionController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockPredictionService = new Mock<IPredictionService>();
            _mockSensorDataService = new Mock<ISensorDataService>();
            _mockLogger = new Mock<ILogger<PredictionController>>();

            _controller = new PredictionController(
                _mockPredictionService.Object,
                _mockSensorDataService.Object,
                _mockLogger.Object
            );
        }

        [Test]
        public async Task GetForecast_ReturnsOk_WhenForecastIsAvailable()
        {
            // Arrange
            var lastSampleTime = DateTime.UtcNow;
            var forecast = new ForecastDTO(120, lastSampleTime);
            _mockPredictionService.Setup(p => p.GetPredictionAsync()).ReturnsAsync(forecast);

            // Act
            var result = await _controller.GetForecast();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.StatusCode, Is.EqualTo(200));

            // Extract the anonymous wrapper object
            var value = okResult.Value;
            var property = value?.GetType().GetProperty("forecastDTO");
            Assert.That(property, Is.Not.Null);

            var forecastValue = property?.GetValue(value) as ForecastDTO;
            Assert.That(forecastValue, Is.EqualTo(forecast));
        }


        [Test]
        public async Task GetForecast_ReturnsNotFound_WhenForecastIsNull()
        {
            // Arrange
            _mockPredictionService.Setup(p => p.GetPredictionAsync()).ReturnsAsync((ForecastDTO?)null);

            // Act
            var result = await _controller.GetForecast();

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task GetForecast_Returns500_WhenExceptionIsThrown()
        {
            // Arrange
            _mockPredictionService.Setup(p => p.GetPredictionAsync()).ThrowsAsync(new Exception("Unexpected"));

            // Act
            var result = await _controller.GetForecast();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.That(objectResult, Is.Not.Null);
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
            Assert.That(objectResult.Value, Is.EqualTo("An internal server error occurred."));
        }
    }
}
