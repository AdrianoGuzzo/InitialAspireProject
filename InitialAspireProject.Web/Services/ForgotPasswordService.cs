namespace InitialAspireProject.Web.Services
{
    public interface IForgotPasswordService
    {
        Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    }

    public class ForgotPasswordService(HttpClient httpClient, ILogger<ForgotPasswordService> logger) : IForgotPasswordService
    {
        public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                await httpClient.PostAsJsonAsync("/auth/forgot-password", new { email }, cancellationToken);

                return new ForgotPasswordResult { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "Network failure during forgot password request for {Email}", email);
                return new ForgotPasswordResult
                {
                    Success = false,
                    Message = "Erro de conexão. Tente novamente mais tarde."
                };
            }
        }
    }
}
