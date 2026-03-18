using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IForgotPasswordService
    {
        Task<ServiceResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    }

    public class ForgotPasswordService(HttpClient httpClient, ILogger<ForgotPasswordService> logger)
        : BaseHttpService(httpClient, logger), IForgotPasswordService
    {
        public Task<ServiceResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            return PostAntiEnumerationAsync("/auth/forgot-password", new ForgotPasswordModel { Email = email }, cancellationToken);
        }
    }
}
