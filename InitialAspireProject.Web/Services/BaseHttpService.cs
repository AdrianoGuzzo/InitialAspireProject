using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services;

public abstract class BaseHttpService(HttpClient httpClient, ILogger logger)
{
    protected HttpClient HttpClient { get; } = httpClient;

    protected Task<ServiceResult> PostWithValidationAsync<T>(string url, T payload, CancellationToken cancellationToken = default)
    {
        return SendWithValidationAsync(HttpMethod.Post, url, payload, cancellationToken);
    }

    protected async Task<ServiceResult> PostAntiEnumerationAsync<T>(string url, T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            await HttpClient.PostAsJsonAsync(url, payload, cancellationToken);
            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Network failure during request to {Url}", url);
            return ServiceResult.Fail("Erro de conexão. Tente novamente mais tarde.");
        }
    }

    protected async Task<ServiceResult> PostWithStatusCheckAsync<T>(string url, T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync(url, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Request to {Url} failed: {Response}", url, body);
                return ServiceResult.Fail(body);
            }

            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Network failure during request to {Url}", url);
            return ServiceResult.Fail("Erro de conexão. Tente novamente mais tarde.");
        }
    }

    protected async Task<ServiceResult> SendWithValidationAsync<T>(HttpMethod method, string url, T payload, CancellationToken cancellationToken = default, string? token = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(payload)
            };
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await HttpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string? message;
                try
                {
                    var errors = await response.Content.ReadFromJsonAsync<ErrorValidation[]>(cancellationToken: cancellationToken);
                    message = errors is not null
                        ? string.Join("\n", errors.Select(x => x.Description))
                        : await response.Content.ReadAsStringAsync(cancellationToken);
                }
                catch (JsonException)
                {
                    message = await response.Content.ReadAsStringAsync(cancellationToken);
                }

                logger.LogError("Validation failed for {Url}: {Errors}", url, message);
                return ServiceResult.Fail(message);
            }

            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Network failure during request to {Url}", url);
            return ServiceResult.Fail("Erro de conexão. Tente novamente mais tarde.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during request to {Url}", url);
            return ServiceResult.Fail("Erro interno do servidor. Tente novamente mais tarde.");
        }
    }
}

public abstract class AuthenticatedHttpService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger logger, ITokenRefreshService? tokenRefreshService = null)
    : BaseHttpService(httpClient, logger)
{
    protected string? GetToken()
    {
        return httpContextAccessor.HttpContext?.Session.GetString(SessionConstants.TokenKey);
    }

    protected HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var token = GetToken();
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    protected async Task<HttpResponseMessage> SendWithAutoRefreshAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && tokenRefreshService is not null)
        {
            if (await tokenRefreshService.TryRefreshAsync())
            {
                using var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
                var newToken = GetToken();
                if (!string.IsNullOrEmpty(newToken))
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                if (request.Content is JsonContent)
                    retryRequest.Content = request.Content;

                response = await HttpClient.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }
}
