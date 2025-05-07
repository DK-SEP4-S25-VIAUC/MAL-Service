using System.Net;
using System.Net.Http.Json;
using API.DataEntities;
using API.IntegrationTests.Helpers;
using API.Services.PredictionService;
using FluentAssertions;
using Moq;
using Xunit;

namespace API.IntegrationTests.Controllers;

public class PredictionControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public PredictionControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;

        // Default mock setup for successful prediction
        _factory.MockPredictionService
            .Setup(x => x.GetPredictionAsync(It.IsAny<double?>()))
            .ReturnsAsync(new ForecastDTO(180));

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPrediction_ReturnsOkWithForecast()
    {
        var response = await _client.GetAsync("/soilhumidity/forecast");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastDTO>();
        forecast.Should().NotBeNull();
        forecast!.next_watering_time.Should().Be(180);
    }

    [Fact]
    public async Task GetPrediction_WhenServiceReturnsNull_ReturnsServiceUnavailable()
    {
        _factory.MockPredictionService
            .Setup(x => x.GetPredictionAsync(It.IsAny<double?>()))
            .ReturnsAsync((ForecastDTO?)null);

        var response = await _client.GetAsync("/soilhumidity/forecast");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetPrediction_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        _factory.MockPredictionService
            .Setup(x => x.GetPredictionAsync(It.IsAny<double?>()))
            .ThrowsAsync(new Exception("Prediction failure"));

        var response = await _client.GetAsync("/soilhumidity/forecast");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetPrediction_WithZeroMinutes_ReturnsImmediateWatering()
    {
        _factory.MockPredictionService
            .Setup(x => x.GetPredictionAsync(It.IsAny<double?>()))
            .ReturnsAsync(new ForecastDTO(0));

        var response = await _client.GetAsync("/soilhumidity/forecast");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastDTO>();
        forecast.Should().NotBeNull();
        forecast!.next_watering_time.Should().Be(0);
    }
}
