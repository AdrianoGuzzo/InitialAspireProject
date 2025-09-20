using InitialAspireProject.ApiCore.Domain;
using InitialAspireProject.ApiCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.ApiCore.Service
{
    public class WeatherForecastService(CoreDbContext context)
    {
        public async Task SaveRangeAsync(List<WeatherForecast> weatherForecasts)
        {
            await context.WeatherForecast.AddRangeAsync(weatherForecasts.Select(WeatherForecastEntity.New));
            await context.SaveChangesAsync();
        }
        public IEnumerable<WeatherForecast> GetList()
        => context.WeatherForecast
            .AsNoTracking()
            .Select(wf => wf.ToDomain())
            .ToList();
    }
}
