using Bogus;
using InitialAspireProject.ApiCore.Domain;

namespace InitialAspireProject.Tests.Builders;

public class WeatherForecastBuilder
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly Faker _faker = new("pt_BR");

    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    private int _temperatureC = 20;
    private string? _summary;

    public WeatherForecastBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    public WeatherForecastBuilder WithTemperatureC(int temperatureC)
    {
        _temperatureC = temperatureC;
        return this;
    }

    public WeatherForecastBuilder WithSummary(string summary)
    {
        _summary = summary;
        return this;
    }

    public WeatherForecast Build() => new()
    {
        Date        = _date,
        TemperatureC = _temperatureC,
        Summary     = _summary ?? _faker.PickRandom(Summaries)
    };

    public static WeatherForecast Default() => new WeatherForecastBuilder().Build();

    public static List<WeatherForecast> BuildList(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WeatherForecastBuilder()
                .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(i)))
                .WithTemperatureC(new Faker().Random.Int(-20, 55))
                .Build())
            .ToList();
}
