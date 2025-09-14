using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace InitialAspireProject.Web
{
    public interface ILoginService
    {
        Task<ResponseToken> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    }

    public class LoginService(HttpClient httpClient, ProtectedLocalStorage localStorage) : ILoginService
    {
        // Exemplo simples: usuário fixo
        public async Task<ResponseToken?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync ("/auth/login", new LoginRequest
            {
                Email = username,
                Password = password
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<ResponseToken>(cancellationToken: cancellationToken);           

            return await Task.FromResult(loginResponse);
        }
    }
    public class ResponseToken {
        public string Token { get; set; }
    }
}
