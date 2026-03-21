namespace InitialAspireProject.Bff.Services;

public interface ICoreProxyService
{
    Task<HttpResponseMessage> GetWeatherAsync(string bearerToken);
}

public class CoreProxyService(HttpClient httpClient, ILogger<CoreProxyService> logger)
    : BackendProxyService(httpClient, logger), ICoreProxyService
{
    public Task<HttpResponseMessage> GetWeatherAsync(string bearerToken)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Get, "/WeatherForecast", bearerToken));
}
