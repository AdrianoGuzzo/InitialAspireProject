using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface ILoginService
    {
        Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    }

    public class LoginService(HttpClient httpClient) : ILoginService
    {
        public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken)
        {
            var response = await httpClient.PostAsJsonAsync("/auth/login", new LoginModel
            {
                Email = username,
                Password = password
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadFromJsonAsync<LoginErrorResponse>(cancellationToken: cancellationToken);
                return new LoginResult { ErrorCode = errorBody?.Code };
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);

            return new LoginResult { Token = loginResponse };
        }
    }

    public class LoginResult
    {
        public LoginResponse? Token { get; set; }
        public string? ErrorCode { get; set; }
        public bool Success => Token is not null;
        public bool IsEmailNotConfirmed => ErrorCode == "EmailNotConfirmed";
    }
}
