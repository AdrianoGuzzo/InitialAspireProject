using InitialAspireProject.ApiCore.Domain;

namespace InitialAspireProject.ApiCore.Entities
{
    public class WeatherForecastEntity
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public string? Summary { get; set; }
        public WeatherForecastEntity()
        {

        }
        private WeatherForecastEntity(WeatherForecast weatherForecast)
        => FromDomain(weatherForecast);

        public static WeatherForecastEntity New(WeatherForecast weatherForecast)
            => new WeatherForecastEntity(weatherForecast);

        private void FromDomain(WeatherForecast weatherForecast)
        {
            Date = weatherForecast.Date;
            TemperatureC = weatherForecast.TemperatureC;
            Summary = weatherForecast.Summary;
        }
        public WeatherForecast ToDomain()
            => new WeatherForecast
            {
                Date = Date,
                Summary = Summary,
                TemperatureC = TemperatureC
            };

    }
}
