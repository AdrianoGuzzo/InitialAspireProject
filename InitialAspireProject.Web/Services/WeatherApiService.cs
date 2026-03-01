namespace InitialAspireProject.Web.Services;

public class WeatherApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<WeatherApiService> logger)
{
    private const string TokenKey = "AuthToken";

    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var token = httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
