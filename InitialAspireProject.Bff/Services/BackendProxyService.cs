using System.Net;
using System.Text;
using System.Text.Json;

namespace InitialAspireProject.Bff.Services;

public abstract class BackendProxyService(HttpClient httpClient, ILogger logger)
{
    protected HttpClient HttpClient { get; } = httpClient;
    protected ILogger Logger { get; } = logger;

    protected HttpRequestMessage CreateForwardRequest(HttpMethod method, string url, string? bearerToken = null, string? acceptLanguage = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (bearerToken is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
        if (acceptLanguage is not null)
            request.Headers.Add("Accept-Language", acceptLanguage);
        return request;
    }

    protected HttpRequestMessage CreateForwardRequest<T>(HttpMethod method, string url, T body, string? bearerToken = null, string? acceptLanguage = null)
    {
        var request = CreateForwardRequest(method, url, bearerToken, acceptLanguage);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        return request;
    }

    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        try
        {
            return await HttpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Backend service unavailable: {Url}", request.RequestUri);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { code = "ServiceUnavailable", message = "Backend service is unavailable." }),
                    Encoding.UTF8,
                    "application/json")
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            Logger.LogError(ex, "Backend service timeout: {Url}", request.RequestUri);
            return new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { code = "GatewayTimeout", message = "Backend service timed out." }),
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
