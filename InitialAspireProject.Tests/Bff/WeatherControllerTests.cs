using System.Net;
using System.Text.Json;
using InitialAspireProject.Bff.Controllers;
using InitialAspireProject.Bff.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InitialAspireProject.Tests.Bff;

public class WeatherControllerTests
{
    private static WeatherController CreateController(Mock<ICoreProxyService> coreProxy, string? bearerToken = null, string? acceptLanguage = null)
    {
        var controller = new WeatherController(coreProxy.Object);
        var httpContext = new DefaultHttpContext();
        if (bearerToken is not null)
            httpContext.Request.Headers.Authorization = $"Bearer {bearerToken}";
        if (acceptLanguage is not null)
            httpContext.Request.Headers.AcceptLanguage = acceptLanguage;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task GetWeather_WithToken_ReturnsWeatherData()
    {
        var proxy = new Mock<ICoreProxyService>();
        var weatherData = new[] { new { date = "2024-01-01", temperatureC = 25, summary = "Warm" } };
        proxy.Setup(x => x.GetWeatherAsync("my-token", It.IsAny<string?>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(weatherData), System.Text.Encoding.UTF8, "application/json")
            });

        var controller = CreateController(proxy, bearerToken: "my-token");
        var result = await controller.GetWeather();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(200, content.StatusCode);
        Assert.Contains("Warm", content.Content);
    }

    [Fact]
    public async Task GetWeather_BackendError_ForwardsStatusCode()
    {
        var proxy = new Mock<ICoreProxyService>();
        proxy.Setup(x => x.GetWeatherAsync("my-token", It.IsAny<string?>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"fail\"}", System.Text.Encoding.UTF8, "application/json")
            });

        var controller = CreateController(proxy, bearerToken: "my-token");
        var result = await controller.GetWeather();

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(500, content.StatusCode);
    }

    [Fact]
    public async Task GetWeather_ForwardsAcceptLanguageHeader()
    {
        var proxy = new Mock<ICoreProxyService>();
        proxy.Setup(x => x.GetWeatherAsync("my-token", "pt-BR"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });

        var controller = CreateController(proxy, bearerToken: "my-token", acceptLanguage: "pt-BR");
        await controller.GetWeather();

        proxy.Verify(x => x.GetWeatherAsync("my-token", "pt-BR"), Times.Once);
    }
}
