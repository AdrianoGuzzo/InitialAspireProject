using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IConfirmEmailService
    {
        Task<ConfirmEmailResult> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default);
        Task<ConfirmEmailResult> ResendActivationAsync(string email, CancellationToken cancellationToken = default);
    }

    public class ConfirmEmailService(HttpClient httpClient, ILogger<ConfirmEmailService> logger) : IConfirmEmailService
    {
        public async Task<ConfirmEmailResult> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await httpClient.PostAsJsonAsync("/auth/confirm-email", new ConfirmEmailModel { Email = email, Token = token }, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("Email confirmation failed: {Response}", body);
                    return new ConfirmEmailResult { Success = false, Message = body };
                }

                return new ConfirmEmailResult { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "Network failure during email confirmation for {Email}", email);
                return new ConfirmEmailResult
                {
                    Success = false,
                    Message = "Erro de conexão. Tente novamente mais tarde."
                };
            }
        }

        public async Task<ConfirmEmailResult> ResendActivationAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                await httpClient.PostAsJsonAsync("/auth/resend-activation", new ForgotPasswordModel { Email = email }, cancellationToken);
                return new ConfirmEmailResult { Success = true };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "Network failure during resend activation for {Email}", email);
                return new ConfirmEmailResult
                {
                    Success = false,
                    Message = "Erro de conexão. Tente novamente mais tarde."
                };
            }
        }
    }
}
