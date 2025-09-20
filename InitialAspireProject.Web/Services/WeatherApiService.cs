using Microsoft.AspNetCore.Authorization;

namespace InitialAspireProject.Web.Services;
public class WeatherApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
{
    [Authorize]
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {

        List<WeatherForecast>? forecasts = null;

        try
        {
            await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/WeatherForecast", cancellationToken))
            {
                if (forecasts?.Count >= maxItems)
                {
                    break;
                }
                if (forecast is not null)
                {
                    forecasts ??= [];
                    forecasts.Add(forecast);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erro ao chamar API: {ex.Message}");
            return [];
        }

        return forecasts?.ToArray() ?? [];
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}