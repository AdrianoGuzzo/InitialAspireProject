using System.Text.Json;
using InitialAspireProject.Shared;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services;

public class WeatherApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<WeatherApiService> logger, ITokenRefreshService tokenRefreshService)
    : AuthenticatedHttpService(httpClient, httpContextAccessor, logger, tokenRefreshService)
{
    public async Task<WeatherForecastDto[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var forecasts = new List<WeatherForecastDto>();

        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/WeatherForecast");
            using var response = await SendWithAutoRefreshAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await foreach (var forecast in JsonSerializer.DeserializeAsyncEnumerable<WeatherForecastDto>(stream, JsonDefaults.Options, cancellationToken: cancellationToken))
            {
                if (forecasts.Count >= maxItems)
                {
                    break;
                }
                if (forecast is not null)
                {
                    forecasts.Add(forecast);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error calling weather API: {Message}", ex.Message);
            return [];
        }

        return forecasts.ToArray();
    }
}
