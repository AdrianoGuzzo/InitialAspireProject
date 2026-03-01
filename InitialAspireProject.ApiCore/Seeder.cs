using InitialAspireProject.ApiCore.Domain;
using InitialAspireProject.ApiCore.Service;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.ApiCore
{
    public class Seeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            await CreateWeatherForecasts(serviceProvider);
        }
        private static async Task CreateWeatherForecasts(IServiceProvider serviceProvider)
        {
            string[] Summaries =
            [
                "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
            ];
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            if (await dbContext.WeatherForecast.AnyAsync()) return;

            var weatherForecastEntities = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToList();

            var weatherForecastService = scope.ServiceProvider.GetRequiredService<WeatherForecastService>();
            await weatherForecastService.SaveRangeAsync(weatherForecastEntities);
        }
    }
}
