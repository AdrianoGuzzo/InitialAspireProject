using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IConfirmEmailService
    {
        Task<ServiceResult> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default);
        Task<ServiceResult> ResendActivationAsync(string email, CancellationToken cancellationToken = default);
    }

    public class ConfirmEmailService(HttpClient httpClient, ILogger<ConfirmEmailService> logger)
        : BaseHttpService(httpClient, logger), IConfirmEmailService
    {
        public Task<ServiceResult> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken = default)
        {
            return PostWithStatusCheckAsync("/auth/confirm-email", new ConfirmEmailModel { Email = email, Token = token }, cancellationToken);
        }

        public Task<ServiceResult> ResendActivationAsync(string email, CancellationToken cancellationToken = default)
        {
            return PostAntiEnumerationAsync("/auth/resend-activation", new ForgotPasswordModel { Email = email }, cancellationToken);
        }
    }
}
