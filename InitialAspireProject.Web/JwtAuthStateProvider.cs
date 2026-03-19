using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace InitialAspireProject.Web
{
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<JwtAuthStateProvider> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const string AuthScheme = "jwt";
        private ClaimsPrincipal? _cachedUser;

        public JwtAuthStateProvider(IHttpContextAccessor httpContextAccessor, ILogger<JwtAuthStateProvider> logger, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task MarkUserAsAuthenticated(string token, string? refreshToken = null)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(SessionConstants.TokenKey, token);
            if (!string.IsNullOrEmpty(refreshToken))
                _httpContextAccessor.HttpContext?.Session.SetString(SessionConstants.RefreshTokenKey, refreshToken);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, AuthScheme);
            var user = new ClaimsPrincipal(identity);

            await _httpContextAccessor.HttpContext!.SignInAsync("Cookies", user);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // HttpContext is null after the SignalR circuit starts; return cached state
            if (_httpContextAccessor.HttpContext is null)
                return new AuthenticationState(_cachedUser ?? new ClaimsPrincipal(new ClaimsIdentity()));

            var savedToken = _httpContextAccessor.HttpContext.Session.GetString(SessionConstants.TokenKey);

            if (string.IsNullOrWhiteSpace(savedToken))
            {
                _cachedUser = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            try
            {
                var claims = ParseClaimsFromJwt(savedToken);
                var identity = new ClaimsIdentity(claims, AuthScheme);
                _cachedUser = new ClaimsPrincipal(identity);
                return new AuthenticationState(_cachedUser);
            }
            catch (TokenExpiredException)
            {
                _logger.LogInformation("JWT token expired, attempting silent refresh");
                try
                {
                    var refreshService = _serviceProvider.GetService<ITokenRefreshService>();
                    if (refreshService is not null && await refreshService.TryRefreshAsync())
                    {
                        var newToken = _httpContextAccessor.HttpContext.Session.GetString(SessionConstants.TokenKey);
                        if (!string.IsNullOrEmpty(newToken))
                        {
                            var newClaims = ParseClaimsFromJwt(newToken);
                            var newIdentity = new ClaimsIdentity(newClaims, AuthScheme);
                            _cachedUser = new ClaimsPrincipal(newIdentity);
                            return new AuthenticationState(_cachedUser);
                        }
                    }
                }
                catch (Exception refreshEx)
                {
                    _logger.LogWarning(refreshEx, "Silent token refresh failed");
                }

                _httpContextAccessor.HttpContext.Session.Remove(SessionConstants.TokenKey);
                _httpContextAccessor.HttpContext.Session.Remove(SessionConstants.RefreshTokenKey);
                _cachedUser = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            catch (Exception ex) when (ex is FormatException || ex is JsonException || ex is IndexOutOfRangeException)
            {
                _logger.LogWarning(ex, "Invalid JWT token found in session; removing it");
                _httpContextAccessor.HttpContext.Session.Remove(SessionConstants.TokenKey);
                _cachedUser = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading authentication state");
                _httpContextAccessor.HttpContext.Session.Remove(SessionConstants.TokenKey);
                _cachedUser = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyUserAuthentication(string token, string? refreshToken = null)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(SessionConstants.TokenKey, token);
            if (!string.IsNullOrEmpty(refreshToken))
                _httpContextAccessor.HttpContext?.Session.SetString(SessionConstants.RefreshTokenKey, refreshToken);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, AuthScheme);
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task NotifyUserLogout()
        {
            _cachedUser = null;
            _httpContextAccessor.HttpContext?.Session.Remove(SessionConstants.TokenKey);
            _httpContextAccessor.HttpContext?.Session.Remove(SessionConstants.RefreshTokenKey);
            if (_httpContextAccessor.HttpContext is not null)
                await _httpContextAccessor.HttpContext.SignOutAsync("Cookies");

            var user = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public string? GetStoredToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(SessionConstants.TokenKey);
        }

        public string? GetStoredRefreshToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(SessionConstants.RefreshTokenKey);
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                throw new FormatException("JWT does not have three parts");

            var jsonBytes = ParseBase64WithoutPadding(parts[1]);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes)
                ?? throw new JsonException("JWT payload could not be deserialized");

            var claims = new List<Claim>();
            foreach (var kvp in keyValuePairs)
            {
                if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                        claims.Add(new Claim(kvp.Key, item.GetString()!));
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value.ToString()!));
                }
            }

            // Enforce token expiry
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim is not null && long.TryParse(expClaim.Value, out var exp))
            {
                var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expiry < DateTimeOffset.UtcNow)
                    throw new TokenExpiredException();
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }

    public class TokenExpiredException : Exception
    {
        public TokenExpiredException() : base("JWT token has expired") { }
    }
}
