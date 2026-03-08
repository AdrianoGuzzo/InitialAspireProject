using Bunit;
using Bunit.TestDoubles;
using InitialAspireProject.Web.Components.Pages;
using InitialAspireProject.Web.Resources;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace InitialAspireProject.Tests.Web;

public class WeatherPageTests : Bunit.TestContext
{
    public WeatherPageTests()
    {
        var localizerMock = new Mock<IStringLocalizer<WebMessages>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
                     .Returns<string>(key => new LocalizedString(key, key));
        Services.AddSingleton(localizerMock.Object);

        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("testuser");
    }

    private void RegisterWeatherService(WeatherForecast[] forecasts)
    {
        var json = JsonSerializer.Serialize(forecasts);
        var handler = new TestHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var mockAccessor = new Mock<IHttpContextAccessor>();
        var mockSession = new Mock<ISession>();
        mockAccessor.Setup(x => x.HttpContext).Returns(new DefaultHttpContext());
        mockAccessor.Setup(x => x.HttpContext!.Session).Returns(mockSession.Object);

        var service = new WeatherApiService(httpClient, mockAccessor.Object, Mock.Of<ILogger<WeatherApiService>>());
        Services.AddSingleton(service);
    }

    [Fact]
    public void Weather_WithForecasts_DisplaysCards()
    {
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 25, "Sunny"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 10, "Rainy"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(2)), -5, "Snowy"),
        };
        RegisterWeatherService(forecasts);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".card.h-100").Count > 0);

        Assert.Equal(3, cut.FindAll(".card.h-100").Count);
    }

    [Fact]
    public void Weather_SunnyForecast_ShowsSunIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 30, "Sunny day")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-sun-fill").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-sun-fill"));
    }

    [Fact]
    public void Weather_RainyForecast_ShowsRainIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 15, "Rainy")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-cloud-rain-fill").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-cloud-rain-fill"));
    }

    [Fact]
    public void Weather_CloudyForecast_ShowsCloudIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, "Cloudy")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-cloud-fill").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-cloud-fill"));
    }

    [Fact]
    public void Weather_SnowyForecast_ShowsSnowIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), -2, "Snowy")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-snow").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-snow"));
    }

    [Fact]
    public void Weather_StormForecast_ShowsLightningIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 22, "Stormy weather")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-cloud-lightning-fill").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-cloud-lightning-fill"));
    }

    [Fact]
    public void Weather_UnknownSummary_ShowsDefaultIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 18, "Mild")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-cloud-sun-fill").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-cloud-sun-fill"));
    }

    [Fact]
    public void Weather_HighTemp_ShowsDangerBadge()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 35, "Sunny")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bg-danger").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bg-danger"));
    }

    [Fact]
    public void Weather_MediumTemp_ShowsWarningBadge()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 22, "Mild")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bg-warning").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bg-warning"));
    }

    [Fact]
    public void Weather_CoolTemp_ShowsSuccessBadge()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 15, "Cool")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bg-success").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bg-success"));
    }

    [Fact]
    public void Weather_ColdTemp_ShowsInfoBadge()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 3, "Cold")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bg-info").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bg-info"));
    }

    [Fact]
    public void Weather_FreezingTemp_ShowsPrimaryBadge()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), -10, "Freezing")]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bg-primary").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bg-primary"));
    }

    [Fact]
    public void Weather_DisplaysStatisticsSummary()
    {
        var forecasts = new[]
        {
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 10, "Cool"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 30, "Hot"),
        };
        RegisterWeatherService(forecasts);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.Markup.Contains("WeeklySummary"));

        Assert.Contains("30", cut.Markup); // Max temp
        Assert.Contains("10", cut.Markup); // Min temp
    }

    [Fact]
    public void Weather_NullSummary_ShowsDefaultIcon()
    {
        RegisterWeatherService([new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 20, null)]);

        var cut = RenderComponent<Weather>();
        cut.WaitForState(() => cut.FindAll(".bi-cloud-sun-fill").Count > 0);

        Assert.NotEmpty(cut.FindAll(".bi-cloud-sun-fill"));
    }
}
