using System.Text.Json;

namespace InitialAspireProject.Web.Services;

public class WeatherApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<WeatherApiService> logger)
    : AuthenticatedHttpService(httpClient, httpContextAccessor, logger)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/WeatherForecast");
            using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var forecast in JsonSerializer.DeserializeAsyncEnumerable<WeatherForecast>(stream, cancellationToken: cancellationToken))
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
            logger.LogError(ex, "Error calling weather API: {Message}", ex.Message);
            return [];
        }

        return forecasts?.ToArray() ?? [];
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
