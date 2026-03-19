using System.Net.Http.Json;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services;

public interface ITokenRefreshService
{
    Task<bool> TryRefreshAsync();
}

public class TokenRefreshService : ITokenRefreshService
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TokenRefreshService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public TokenRefreshService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TokenRefreshService> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> TryRefreshAsync()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session is null) return false;

        var refreshToken = session.GetString(SessionConstants.RefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken)) return false;

        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(5)))
            return false;

        try
        {
            // Re-check after acquiring lock — another thread may have refreshed already
            refreshToken = session.GetString(SessionConstants.RefreshTokenKey);
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var response = await _httpClient.PostAsJsonAsync("/auth/refresh",
                new RefreshTokenRequest { RefreshToken = refreshToken });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
                return false;
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse is null) return false;

            session.SetString(SessionConstants.TokenKey, loginResponse.Token);
            if (!string.IsNullOrEmpty(loginResponse.RefreshToken))
                session.SetString(SessionConstants.RefreshTokenKey, loginResponse.RefreshToken);

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Network error during token refresh");
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
