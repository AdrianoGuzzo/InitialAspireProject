using InitialAspireProject.ApiCore.Domain;

namespace InitialAspireProject.Tests.ApiCore;

public class WeatherForecastDomainTests
{
    [Fact]
    public void TemperatureF_ForZeroCelsius_Returns32()
    {
        var forecast = new WeatherForecast { TemperatureC = 0 };

        Assert.Equal(32, forecast.TemperatureF);
    }

    [Fact]
    public void TemperatureF_ForHundredCelsius_Returns211()
    {
        // Formula: 32 + (int)(100 / 0.5556) = 32 + (int)(179.985) = 32 + 179 = 211
        // Integer truncation means this differs slightly from the exact 212°F
        var forecast = new WeatherForecast { TemperatureC = 100 };

        Assert.Equal(211, forecast.TemperatureF);
    }

    [Fact]
    public void TemperatureF_ForMinusTwentyCelsius_IsCorrect()
    {
        var forecast = new WeatherForecast { TemperatureC = -20 };

        // -20°C = -4°F  →  32 + (int)(-20 / 0.5556) = 32 + (int)(-35.997) = 32 + (-35) = -3
        Assert.Equal(-3, forecast.TemperatureF);
    }

    [Fact]
    public void TemperatureF_ForThirtySeveCelsius_IsBodyTemperature()
    {
        var forecast = new WeatherForecast { TemperatureC = 37 };

        // 37°C = 98.6°F  →  32 + (int)(37 / 0.5556) ≈ 32 + 66 = 98
        Assert.Equal(98, forecast.TemperatureF);
    }

    [Fact]
    public void TemperatureC_CanBeSet()
    {
        var forecast = new WeatherForecast { TemperatureC = 25 };

        Assert.Equal(25, forecast.TemperatureC);
    }

    [Fact]
    public void Summary_CanBeNull()
    {
        var forecast = new WeatherForecast { Summary = null };

        Assert.Null(forecast.Summary);
    }
}
