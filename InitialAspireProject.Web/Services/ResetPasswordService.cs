namespace InitialAspireProject.Web.Services
{
    public interface IResetPasswordService
    {
        Task<ResetPasswordResult> ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default);
    }

    public class ResetPasswordService(HttpClient httpClient, ILogger<ResetPasswordService> logger) : IResetPasswordService
    {
        public async Task<ResetPasswordResult> ResetPasswordAsync(string email, string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
        {
            ErrorValidation[]? errorValidations = null;
            try
            {
                var response = await httpClient.PostAsJsonAsync("/auth/reset-password", new
                {
                    email,
                    token,
                    newPassword,
                    confirmPassword
                }, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    errorValidations = await response.Content.ReadFromJsonAsync<ErrorValidation[]>(cancellationToken: cancellationToken);
                    throw new Exception("Validation Error");
                }

                return new ResetPasswordResult { Success = true };
            }
            catch (Exception ex)
            {
                if (errorValidations is not null)
                {
                    var validationError = string.Join("\n", errorValidations.Select(x => x.Description).ToArray());
                    logger.LogError(ex, "Reset password validation failed: {Errors}", validationError);
                    return new ResetPasswordResult { Success = false, Message = validationError };
                }

                logger.LogError(ex, "Unexpected reset password error: {Message}", ex.Message);
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = "Erro interno do servidor. Tente novamente mais tarde."
                };
            }
        }
    }
}
