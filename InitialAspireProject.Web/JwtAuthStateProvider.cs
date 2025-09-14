using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace InitialAspireProject.Web
{
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string TokenKey = "AuthToken";

        public JwtAuthStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task MarkUserAsAuthenticated(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(TokenKey, token);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "Cookies");
            var user = new ClaimsPrincipal(identity);

            // Cria cookie de autenticação
            await _httpContextAccessor.HttpContext!.SignInAsync("Cookies", user);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Busca o token da sessão
            var savedToken = _httpContextAccessor.HttpContext?.Session.GetString(TokenKey);

            if (string.IsNullOrWhiteSpace(savedToken))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            try
            {
                var claims = ParseClaimsFromJwt(savedToken);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                // Token inválido, remove da sessão
                _httpContextAccessor.HttpContext?.Session.Remove(TokenKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            // Salva na sessão
            _httpContextAccessor.HttpContext?.Session.SetString(TokenKey, token);

            var claims = ParseClaimsFromJwt(token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public void NotifyUserLogout()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(TokenKey);
            _httpContextAccessor.HttpContext?.SignOutAsync("Cookies");

            var user = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public string? GetStoredToken()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
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

}
