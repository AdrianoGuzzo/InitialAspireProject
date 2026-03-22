namespace InitialAspireProject.Bff.Services;

public interface ICoreProxyService
{
    Task<HttpResponseMessage> GetWeatherAsync(string bearerToken, string? acceptLanguage = null);
}

public class CoreProxyService(HttpClient httpClient, ILogger<CoreProxyService> logger)
    : BackendProxyService(httpClient, logger), ICoreProxyService
{
    public Task<HttpResponseMessage> GetWeatherAsync(string bearerToken, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Get, "/WeatherForecast", bearerToken, acceptLanguage));
}
