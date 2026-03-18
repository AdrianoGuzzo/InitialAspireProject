using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IResetPasswordService
    {
        Task<ServiceResult> ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default);
    }

    public class ResetPasswordService(HttpClient httpClient, ILogger<ResetPasswordService> logger)
        : BaseHttpService(httpClient, logger), IResetPasswordService
    {
        public Task<ServiceResult> ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
        {
            return PostWithValidationAsync("/auth/reset-password", new ResetPasswordModel
            {
                Email = email,
                Token = token,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            }, cancellationToken);
        }
    }
}
