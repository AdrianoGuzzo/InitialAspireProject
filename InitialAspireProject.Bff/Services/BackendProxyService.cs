using System.Text;
using System.Text.Json;

namespace InitialAspireProject.Bff.Services;

public abstract class BackendProxyService(HttpClient httpClient, ILogger logger)
{
    protected HttpClient HttpClient { get; } = httpClient;
    protected ILogger Logger { get; } = logger;

    protected HttpRequestMessage CreateForwardRequest(HttpMethod method, string url, string? bearerToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (bearerToken is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        return request;
    }

    protected HttpRequestMessage CreateForwardRequest<T>(HttpMethod method, string url, T body, string? bearerToken = null)
    {
        var request = CreateForwardRequest(method, url, bearerToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        return request;
    }
}
